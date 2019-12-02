using Microsoft.ApplicationInsights;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using MyMonthlyBlogArticle.Bot.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MyMonthlyBlogArticle.Bot.Bots
{
    public class MyMonthlyBlogArticleBot : ActivityHandler
    {
        private TelemetryClient Telemetry { get; set; }

        private string WelcomeMessage = "Hello and welcome! I'm the 'Microsoft Azure News & Updates' Bot!";

        private string HelpMessage
        {
            get
            {
                return $"You can search: \n* by month: *{DateTime.UtcNow.ToString("yyyy-MM")}* \n* by date: *{DateTime.UtcNow.ToString("yyyy-MM-dd")}* \n* by keyword: *Functions*, *\"API Management\"*, *DevOps*, *AKS | Kubernetes*, etc.";
            }
        }

        private string AzureSearchServiceName { get; set; }
        private string AzureSearchIndexName { get; set; }
        private ISearchIndexClient SearchIndexClient { get; set; }

        public MyMonthlyBlogArticleBot(IConfiguration configuration, TelemetryClient telemetryClient)
        {
            AzureSearchServiceName = configuration["AZURE_SEARCH_SERVICE_NAME"];
            AzureSearchIndexName = configuration["AZURE_SEARCH_INDEX_NAME"];
            SearchIndexClient = new SearchIndexClient(AzureSearchServiceName, AzureSearchIndexName, new SearchCredentials(configuration["AZURE_SEARCH_SERVICE_QUERY_API_KEY"]));
            Telemetry = telemetryClient;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            if (Regex.IsMatch(text, "^Hello|hello|Hi|hi|Hey|hey|Help|help|What|what"))
            {
                var properties = new Dictionary<string, string> { { "Text", text } };
                Telemetry.TrackEvent("Hello", properties);
                await turnContext.SendActivityAsync(HelpMessage, cancellationToken: cancellationToken);
            }
            else if (Regex.IsMatch(text, @"\d{4}\-\d{2}\-\d{2}"))
            {
                await PostRssFeedsAsync(turnContext, cancellationToken, date: text);
            }
            else if (Regex.IsMatch(text, @"\d{4}\-\d{2}"))
            {
                await PostRssFeedsAsync(turnContext, cancellationToken, month: text);
            }
            else
            {
                await PostRssFeedsAsync(turnContext, cancellationToken, text: text);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(HelpMessage, cancellationToken: cancellationToken);
                }
            }
        }

        public async Task PostRssFeedsAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, string date = null, string month = null, string text = null)
        {
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var results = GetRssFeeds(date, month, text);
            var resultsCount = results.Count.Value;
            var timerElapsed = timer.ElapsedMilliseconds;
            TrackEvent(date, month, text, resultsCount, timerElapsed);

            if (resultsCount > 0)
            {
                var builder = new StringBuilder();
                builder.Append($"{resultsCount} results found:");
                foreach (var item in results.Results)
                {
                    // Need 2 spaces before each line for Teams channel.
                    builder.Append($"  \n[{item.Document.Date}]({item.Document.Link}) - {item.Document.Title}");
                }
                await turnContext.SendActivityAsync($"{builder.ToString()}", cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"No results found. \n{HelpMessage}", cancellationToken: cancellationToken);
            }
        }

        private DocumentSearchResult<Feed> GetRssFeeds(string date = null, string month = null, string text = null)
        {
            var searchText = text;
            var top = 1000;//that's the max allowed by the Azure Search API, otherwise by default it's 50. I don't think any search by bot at least will have more than 1000 documents returned for now... to keep in mind. Alternative: do paging.
            var parameters = new SearchParameters() { OrderBy = new[] { "Date desc" }, Top = top, IncludeTotalResultCount = true };
            if (!string.IsNullOrEmpty(month))
            {
                searchText = "*";
                parameters.Filter = $"PartitionKey eq '{month}'";
            }
            else if (!string.IsNullOrEmpty(date))
            {
                searchText = "*";
                parameters.Filter = $"Date eq '{date}'";
            }
            return SearchIndexClient.Documents.Search<Feed>(searchText, parameters);
        }

        private void TrackEvent(string date, string month, string text, long resultsCount, long elapsedTime)
        {
            var properties = new Dictionary<string, string> {
                {"SearchServiceName", AzureSearchServiceName},
                {"IndexName", AzureSearchIndexName},
                {"QueryTerms", date ?? month ?? text},
                {"ResultCount", resultsCount.ToString()},
                {"SearchType", !string.IsNullOrEmpty(date) ? "Date" : !string.IsNullOrEmpty(month) ? "Month" : "Text"},
                {"SearchTimeElapsed", elapsedTime.ToString()}
            };
            Telemetry.TrackEvent("Search", properties);
        }
    }
}