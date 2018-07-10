using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            CheckInit(context);

            context.Wait(MessageReceivedAsync);
        }

        private void CheckInit(IDialogContext context)
        {
            if (!context.ConversationData.GetValueOrDefault<bool>("init"))
            {
                context.ConversationData.SetValue("init", true);
                context.PostAsync("Hi! I'll post some random question every morning. Good luck!");
                context.PostAsync(Constants.HELP);
                context.PostAsync("Good luck!");
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity message = await argument;

            ConversationStarter.SaveConversation(message);

            context.Call(new QuestionDialog(), After);
        }

        private async Task After(IDialogContext context, IAwaitable<object> result)
        {
            context.Wait(MessageReceivedAsync);
        }
    }
}