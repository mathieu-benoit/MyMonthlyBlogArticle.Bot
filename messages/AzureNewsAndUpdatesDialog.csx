#r "Microsoft.WindowsAzure.Storage"

#load "FeedEntity.csx"

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

static Regex PartitionKeyRegex = new Regex(@"(\d{4}\-\d{2})");
static Regex DateRegex = new Regex(@"(\d{4}\-\d{2}\-\d{2})");

[Serializable]
public class AzureNewsAndUpdatesDialog : IDialog<object>
{
    public Task StartAsync(IDialogContext context)
    {
        try
        {
            context.Wait(MessageReceivedAsync);
        }
        catch (OperationCanceledException error)
        {
            return Task.FromCanceled(error.CancellationToken);
        }
        catch (Exception error)
        {
            return Task.FromException(error);
        }
        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        var activity = await argument;
        var storageAccountConnectionString = Environment.GetEnvironmentVariable("RssFeedsTableStorageConnectionString");
        var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        var table = tableClient.GetTableReference("RssFeeds");

        var query = new TableQuery<FeedEntity>().Where(GenerateFilterCondition(activity));
        var results = table.ExecuteQuery(query).OrderByDescending(f => f.Date);
        var resultsCount = results.Count();
        if(resultsCount > 0)
        {
            await context.PostAsync($"{resultsCount} results found.");
        }
        else
        {
            await context.PostAsync($"No results found. You could search: **by month**: {DateTime.UtcNow.ToString("yyyy-MM")}; **by date**: {DateTime.UtcNow.ToString("yyyy-MM-dd")}; or **by text**: Functions, API Management, VSTS, DevOps, etc.");
        }
        context.Wait(MessageReceivedAsync);
    }

    private string GenerateFilterCondition(IMessageActivity activity)
    {
        if(PartitionKeyRegex.IsMatch(activity.Text))
        {
            return TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{activity.Text}");
        }
        else if(DateRegex.IsMatch(activity.Text))
        {
            return TableQuery.GenerateFilterCondition("Date", QueryComparisons.Equal, $"{activity.Text}");
        }
        return string.Empty;
    }
}
