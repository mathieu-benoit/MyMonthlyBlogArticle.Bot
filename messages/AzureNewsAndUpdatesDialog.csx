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
    [ScorableGroup(0)]
    public async Task Hello(IDialogContext context, IActivity activity)
    {
        telemetry.TrackEvent($"ByHello-{activity.AsMessageActivity()?.Text}");
        await context.PostAsync($"Hi! I'm the 'Microsoft Azure News & Updates' Bot.\n{HelpMessage}");
    }

    [RegexPattern(@"\d{4}\-\d{2}\-\d{2}")]
    [ScorableGroup(1)]
    public async Task ByDate(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        telemetry.TrackEvent($"ByDate-{message}");
        await PostRssFeedsAsync(context, date: message);
    }

    [RegexPattern(@"\d{4}\-\d{2}")]
    [ScorableGroup(2)]
    public async Task ByMonth(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        telemetry.TrackEvent($"ByMonth-{message}");
        await PostRssFeedsAsync(context, month: message);
    }

    [MethodBind]
    [ScorableGroup(3)]
    public async Task Default(IDialogContext context, IActivity activity)
    {
        var message = activity.AsMessageActivity()?.Text;
        telemetry.TrackEvent($"ByText-{message}");
        await PostRssFeedsAsync(context, text: message);
    }

    public async Task PostRssFeedsAsync(IDialogContext context, string date = null, string month = null, string text = null)
    {
        var results = GetRssFeeds(date, month, text);
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

    private IEnumerable<FeedEntity> GetRssFeeds(string date = null, string month = null, string text = null)
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
        var startTime = DateTime.UtcNow;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var storageAccountConnectionString = Environment.GetEnvironmentVariable("RssFeedsTableStorageConnectionString");
        var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        var table = tableClient.GetTableReference("RssFeeds");
        var query = new TableQuery<FeedEntity>().Where(filterCondition);
        IEnumerable<FeedEntity> results = table.ExecuteQuery(query).OrderByDescending(f => f.Date);
        if(string.IsNullOrEmpty(filterCondition))
        {
            results = from feedEntity 
                        in results 
                        where feedEntity.Title.ToLower().Contains(text.ToLower())
                            || feedEntity.Link.Contains(text.Replace(" ", string.Empty).ToLower())
                        select feedEntity;
        }
        telemetry.TrackDependency("TableStorage", "GetRssFeeds", startTime, timer.Elapsed, true);
        return results;
    }
}
