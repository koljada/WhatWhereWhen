using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Autofac;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Sample.SimpleEchoBot;

namespace Microsoft.Bot.Sample.SimpleEchoBot.Dialogs
{
    public class ConversationStarter
    {
        private static string ConversationReference;

        public static bool IsSet => !string.IsNullOrEmpty(ConversationReference);

        public static void CheckReference(IMessageActivity message)
        {
            if (!IsSet)
            {
                ConversationReference conversationReference = message.ToConversationReference();
                ConversationReference = JsonConvert.SerializeObject(conversationReference);
            }
        }

        public static async Task Resume()
        {
            Activity message = JsonConvert.DeserializeObject<ConversationReference>(ConversationReference).GetPostToBotMessage();
            ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

            using (ILifetimeScope scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
            {
                IBotData botData = scope.Resolve<IBotData>();
                await botData.LoadAsync(CancellationToken.None);

                IDialogTask task = scope.Resolve<IDialogTask>();

                RootDialog dialog = new RootDialog();
                task.Call(dialog.Void<object, IMessageActivity>(), null);

                await task.PollAsync(CancellationToken.None);

                //flush dialog stack
                await botData.FlushAsync(CancellationToken.None);
            }
        }
    }
}