using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            if (Regex.IsMatch(text, "^Hello|hello|Hi|hi|Hey|hey|Help|help|What|what"))
            {
                await turnContext.SendActivityAsync(HelpMessage, cancellationToken: cancellationToken);
            }
            else if (Regex.IsMatch(text, @"\d{4}\-\d{2}\-\d{2}"))
            {
                await turnContext.SendActivityAsync($"Date: {text}", cancellationToken: cancellationToken);
            }
            else if (Regex.IsMatch(text, @"\d{4}\-\d{2}"))
            {
                await turnContext.SendActivityAsync($"Month: {text}", cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"Text: {text}", cancellationToken: cancellationToken);
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
    }
}