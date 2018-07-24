#load "FeedEntity.csx"

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;

public static var telemetry = new TelemetryClient() 
{
    InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
};

static string AzureSearchServiceName = Environment.GetEnvironmentVariable("AzureSearchServiceName");
static string AzureSearchIndexName = Environment.GetEnvironmentVariable("AzureSearchIndexName");
static string AzureSearchServiceQueryApiKey = Environment.GetEnvironmentVariable("AzureSearchServiceQueryApiKey");

private static ISearchIndexClient GetSearchIndexClient()
{
    return new SearchIndexClient(AzureSearchServiceName, AzureSearchIndexName, new SearchCredentials(AzureSearchServiceQueryApiKey));
}

[Serializable]
public class AzureNewsAndUpdatesDialog : DispatchDialog<object>
{
    private string HelpMessage
    {
        get
        {
            return $"You can search: \n* by month: *{DateTime.UtcNow.ToString("yyyy-MM")}* \n* by date: *{DateTime.UtcNow.ToString("yyyy-MM-dd")}* \n* by text: *Functions*, *\"API Management\"*, *VSTS*, *DevOps*, *AKS | Kubernetes*, etc.";
        }
    }

    [RegexPattern("^Hello|hello")]
    [RegexPattern("^Hi|hi")]
    [RegexPattern("^Hey|hey")]
    [RegexPattern("^Help|help")]
    [RegexPattern("^What|what")]
    [ScorableGroup(0)]
    public async Task Hello(IDialogContext context, IActivity activity)
    {
        var properties = new Dictionary<string, string> {{"Text", activity.AsMessageActivity()?.Text}};
        telemetry.TrackEvent("Hello", properties);
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
        var results = GetRssFeeds(date, month, text);
        var timerElapsed = timer.ElapsedMilliseconds;
        var resultsCount = results.Count.Value;
        TrackEvent(date, month, text, resultsCount, timerElapsed);
        
        if(resultsCount > 0)
        {
            var builder = new StringBuilder();
            builder.Append($"{resultsCount} results found:");
            foreach(var item in results.Results)
            {
                builder.Append($"\n[{item.Document.Date}]({item.Document.Link}) - {item.Document.Title}");
            }
            await context.PostAsync($"{builder.ToString()}");
        }
        else
        {
            await context.PostAsync($"No results found. \n{HelpMessage}");
        }
    }
    
    private DocumentSearchResult<Feed> GetRssFeeds(string date = null, string month = null, string text = null)
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
            parameters.Filter = $"Date eq '{date}'";
        }
        return searchIndexClient.Documents.Search<Feed>(searchText, parameters);
    }
    
    private static void TrackEvent(string date, string month, string text, long resultsCount, long elapsedTime)
    {
        var properties = new Dictionary<string, string> {
            {"SearchServiceName", AzureSearchServiceName},
            {"IndexName", AzureSearchIndexName},
            {"QueryTerms", date ?? month ?? text},
            {"ResultCount", resultsCount.ToString()},
            {"SearchType", !string.IsNullOrEmpty(date) ? "Date" : !string.IsNullOrEmpty(month) ? "Month" : "Text"},
            {"SearchTimeElapsed", elapsedTime.ToString()}
        };
        telemetry.TrackEvent("Search", properties);
    }
}
