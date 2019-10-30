using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MyMonthlyBlogArticle.Bot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class MyMonthlyBlogArticleBot : IBot
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public MyMonthlyBlogArticleBot()
        {
        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    //log.Info($"ActivityTypes.Message - {activity.Text}");
                    //await Conversation.SendAsync(activity, () => new AzureNewsAndUpdatesDialog());
                    await turnContext.SendActivityAsync($"Hello World, you said {turnContext.Activity.Text}", cancellationToken: cancellationToken);
                    break;
                case ActivityTypes.ConversationUpdate:
                    await turnContext.SendActivityAsync($"Welcome!", cancellationToken: cancellationToken);
                    break;
                default:
                    //log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                    break;
            }
        }
    }
}