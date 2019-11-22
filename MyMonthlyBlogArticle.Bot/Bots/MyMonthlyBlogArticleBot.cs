using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MyMonthlyBlogArticle.Bot.Bots
{
    public class MyMonthlyBlogArticleBot : ActivityHandler
    {
        private string WelcomeMessage = "Hello and welcome! I'm the 'Microsoft Azure News & Updates' Bot!";

        private string HelpMessage
        {
            get
            {
                return $"You can search: \n* by month: *{DateTime.UtcNow.ToString("yyyy-MM")}* \n* by date: *{DateTime.UtcNow.ToString("yyyy-MM-dd")}* \n* by text: *Functions*, *\"API Management\"*, *DevOps*, *AKS | Kubernetes*, etc.";
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            await turnContext.SendActivityAsync($"Echo: {text}", cancellationToken: cancellationToken);
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
    }
}