#r "Microsoft.WindowsAzure.Storage"

#load "FeedEntity.csx"

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Microsoft.ApplicationInsights;

public static var telemetry = new TelemetryClient() 
{
    InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
};

private static bool IsAzureSearchEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURESEARCH_ENABLED"));
private static string AzureSearchServiceName = Environment.GetEnvironmentVariable("AzureSearchServiceName");
private static string AzureSearchIndexName = Environment.GetEnvironmentVariable("AzureSearchIndexName");

private static CloudTable GetRssFeedsCloudTable()
{
    var storageAccountConnectionString = Environment.GetEnvironmentVariable("RssFeedsTableStorageConnectionString");
    var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
    var tableClient = storageAccount.CreateCloudTableClient();
    return tableClient.GetTableReference("RssFeeds");
}

private static ISearchIndexClient GetSearchIndexClient()
{
    var searchServiceQueryApiKey = Environment.GetEnvironmentVariable("AzureSearchServiceQueryApiKey");
    var indexClient = new SearchIndexClient(AzureSearchServiceName, AzureSearchIndexName, new SearchCredentials(searchServiceQueryApiKey));
    return indexClient;   
}

[Serializable]
public class AzureNewsAndUpdatesDialog : DispatchDialog<object>
{
    private string HelpMessage
    {
        get
        {
            return $"You can search: \n* by month: '{DateTime.UtcNow.ToString("yyyy-MM")}' \n* by date: '{DateTime.UtcNow.ToString("yyyy-MM-dd")}' \n* by text: 'Functions', 'API Management', 'VSTS', 'DevOps', etc.";
        }
    }

    [RegexPattern("^Hello|hello")]
    [RegexPattern("^Hi|hi")]
    [RegexPattern("^Hey|hey")]
    [RegexPattern("^Help|help")]
    [RegexPattern("^What|what")]
    [RegexPattern("*")]//otherwise it will return more than 1000 documents by Azure Search which is not handles for now, and we don't want that with this Bot for now.
    [ScorableGroup(0)]
    public async Task Hello(IDialogContext context, IActivity activity)
    {
        var properties = new Dictionary<string, string> {{"Text", activity.AsMessageActivity()?.Text}};
        telemetryClient.TrackEvent("Hello", properties);
        await context.PostAsync($"Hi! I'm the 'Microsoft Azure News & Updates' Bot.\n{HelpMessage}");
    }

    [RegexPattern(@"\d{4}\-\d{2}\-\d{2}")]
    [ScorableGroup(1)]
    public async Task ByDate(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        await PostRssFeedsAsync(context, date: message);
    }

    [RegexPattern(@"\d{4}\-\d{2}")]
    [ScorableGroup(2)]
    public async Task ByMonth(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        await PostRssFeedsAsync(context, month: message);
    }

    [MethodBind]
    [ScorableGroup(3)]
    public async Task Default(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        await PostRssFeedsAsync(context, text: message);
    }

    public async Task PostRssFeedsAsync(IDialogContext context, string date = null, string month = null, string text = null)
    {
        var startTime = DateTime.UtcNow;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        IEnumerable<IFeedEntity> results = null;
        if(IsAzureSearchEnabled)
        {
            results = GetRssFeedsFromAzureSearch(date, month, text);
            var timerElapsed = timer.ElapsedMilliseconds;
            TrackEventForSearch(date, month, text, results.Count.Value, timerElapsed);
            telemetry.TrackDependency("Search", "GetRssFeeds", startTime, timerElapsed, true);//to remove?
        }
        else
        {
            results = GetRssFeedsFromAzureTableStorage(date, month, text);
            var timerElapsed = timer.ElapsedMilliseconds;
            TrackEvent(null, null, text, results.Count.Value, timerElapsed);
            telemetry.TrackDependency("Table", "GetRssFeeds", startTime, timerElapsed, true);//to remove?
        }
        
        if(results.Count() > 0)
        {
            var builder = new StringBuilder();
            builder.Append($"{results.Count()} results found:");
            foreach(var feed in results)
            {
                builder.Append($"\n[{feed.Date}]({feed.Link}) - {feed.Title}");
            }
            await context.PostAsync($"{builder.ToString()}");
        }
        else
        {
            await context.PostAsync($"No results found. \n{HelpMessage}");
        }
    }
    
    private IEnumerable<IFeedEntity> GetRssFeedsFromAzureSearch(string date = null, string month = null, string text = null)
    {
        var searchIndexClient = GetSearchIndexClient();
        var searchText = text;
        var top = 1000;//that's the max allowed by the Azure Search API, otherwise by default it's 50. I don't think any search by bot at least will have more than 1000 documents returned for now... to keep in mind. Alternative: do paging.
        var parameters = new SearchParameters() { OrderBy = new[] { "Date desc" }, Top = top, IncludeTotalResultCount = true };
        if(!string.IsNullOrEmpty(month))
        {
            searchText = "*";
            parameters.Filter = $"PartitionKey eq '{month}'";
        }
        else if(!string.IsNullOrEmpty(date))
        {
            searchText = "*";
            parameters.Filter = = $"Date eq '{date}'";
        }
        var results = searchIndexClient.Documents.Search<FeedEntityForSearch>(searchText, parameters);
        return results;
    }

    private IEnumerable<IFeedEntity> GetRssFeedsFromAzureTableStorage(string date = null, string month = null, string text = null)
    {
        var filterCondition = string.Empty;
        if(!string.IsNullOrEmpty(month))
        {
            filterCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{month}");
        }
        else if(!string.IsNullOrEmpty(date))
        {
            filterCondition = TableQuery.GenerateFilterCondition("Date", QueryComparisons.Equal, $"{date}");
        }
        var table = GetRssFeedsCloudTable();
        var query = new TableQuery<FeedEntity>().Where(filterCondition);
        IEnumerable<FeedEntityForTable> results = table.ExecuteQuery(query).OrderByDescending(f => f.Date);
        if(string.IsNullOrEmpty(filterCondition))
        {
            results = from feedEntity 
                        in results 
                        where feedEntity.Title.ToLower().Contains(text.ToLower())
                            || feedEntity.Link.Contains(text.Replace(" ", string.Empty).ToLower())
                        select feedEntity;
        }
        return results;
    }
    
    private static void TrackEventForSearch(string date, string month, string text, long resultsCount, long elapsedTime)
    {
        var properties = new Dictionary<string, string> {
            {"SearchServiceName", AzureSearchServiceName},
            {"IndexName", AzureSearchIndexName},
            {"QueryTerms", date ?? month ?? text},
            {"ResultCount", resultsCount.ToString()},
            {"SearchType", !string.IsNullOrEmpty(date) ? "Date" : !string.IsNullOrEmpty(month) ? "Month" : "Text"},
            {"SearchTimeElapsed", elapsedTime.ToString()}
        };
        telemetryClient.TrackEvent("Search", properties);
    }
    
    private static void TrackEventForTable(string date, string month, string text, long resultsCount, long elapsedTime)
    {
        var properties = new Dictionary<string, string> {
            {"QueryTerms", date ?? month ?? text},
            {"ResultCount", resultsCount.ToString()},
            {"SearchType", !string.IsNullOrEmpty(date) ? "Date" : !string.IsNullOrEmpty(month) ? "Month" : "Text"},
            {"SearchTimeElapsed", elapsedTime.ToString()}
        };
        telemetryClient.TrackEvent("Table", properties);
    }
}
