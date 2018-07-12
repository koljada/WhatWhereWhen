using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            Trace.TraceInformation($"RootDialog.StartAsync");

            await CheckInit(context);

            context.Wait(MessageReceivedAsync);
        }

        private async Task CheckInit(IDialogContext context)
        {
            if (!context.ConversationData.GetValueOrDefault<bool>("init"))
            {
                Trace.TraceInformation($"RootDialog.CheckInit - a new dialog");

                context.ConversationData.SetValue("init", true);
                await context.PostAsync("Hi! I'll post some random question every morning.");
                await context.PostAsync(Constants.HELP);
                await context.PostAsync("*All questions are taken from* "+ "https://db.chgk.info/");
                await context.PostAsync("Good luck ;-)");
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity message = await argument;

            Trace.TraceInformation($"RootDialog.MessageReceivedAsync: text: {message.Text}; " +
                $"conversation: {message.Conversation.Id}, {message.Conversation.Name}, {message.Conversation.IsGroup}; " +
                $"from: {message.From.Id}, {message.From.Name}; type: {message.Type}; channel: {message.ChannelId}");

            ConversationStarter.SaveConversation(message);

            context.Call(new QuestionDialog(), After);
        }

        private async Task After(IDialogContext context, IAwaitable<object> result)
        {
            Trace.TraceInformation("RootDialog.After");

            context.Wait(MessageReceivedAsync);
        }
    }
}