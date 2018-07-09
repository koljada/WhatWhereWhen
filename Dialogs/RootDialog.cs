using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SimpleEchoBot.Dialogs;
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
            bool init =  context.ConversationData.GetValueOrDefault<bool>("init");
            if (!init)
            {
                context.ConversationData.SetValue("init", true);
                context.PostAsync("Hi! I'll post some random question every morning. Good luck!");                
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity message = await argument;

            string text = message.Text.Trim().ToLower();

            if (text == "help")
            {
                await context.PostAsync("Commands: " + Environment.NewLine +
                    "\t\t  - type `question` to get a new question;" + Environment.NewLine +
                    "\t\t  - type `answer` to get an answer to the current question;" + Environment.NewLine);

                context.Wait(MessageReceivedAsync);
            }
            else
            {
                ConversationStarter.SaveConversation(message);
                context.Call(new QuestionDialog(), After);
            }
        }

        private async Task After(IDialogContext context, IAwaitable<object> result)
        {
            context.Wait(MessageReceivedAsync);
        }
    }
}