#r "Microsoft.WindowsAzure.Storage"

#load "FeedEntity.csx"

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

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
        var query = new TableQuery<FeedEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{activity.Text}"));
        var results = table.ExecuteQuery(query).OrderByDescending(f => f.Date);
        var resultsCount = results.Count();
        if(resultsCount > 0)
        {
            await context.PostAsync($"{resultsCount} has been found.");
        }
        else
        {
            await context.PostAsync($"No results found. You could search: **by month**: 2018-04; **by date**: 2018-04-03; or **by text**: Functions, API Management, VSTS, DevOps, etc.");
        }
        context.Wait(MessageReceivedAsync);
    }
}
