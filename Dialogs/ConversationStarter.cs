using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Autofac;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEchoBot.Dialogs
{
    public class ConversationStarter
    {
        private static CloudTable GetTable()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("Conversations");
            table.CreateIfNotExists();

            return table;
        }

        public static void SaveConversation(IMessageActivity message)
        {
            CloudTable table = GetTable();

            ConversationHistory conversation = new ConversationHistory(message);

            TableOperation insertOperation = TableOperation.InsertOrReplace(conversation);
            table.Execute(insertOperation);
        }

        private static IList<ConversationHistory> GetConversations()
        {
            try
            {
                CloudTable table = GetTable();

                var histories = table.CreateQuery<ConversationHistory>().ToList();

                return histories;
            }
            catch (Exception ex)
            {
                return new List<ConversationHistory>();
            }
        }

        public static async Task Resume()
        {
            foreach (ConversationHistory history in GetConversations())
            {
                await Resume(history);
            }
        }

        public static async Task Resume(ConversationHistory history)
        {
            try
            {
                Activity message = JsonConvert.DeserializeObject<ConversationReference>(history.Conversation).GetPostToBotMessage();
                ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

                using (ILifetimeScope scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    IBotData botData = scope.Resolve<IBotData>();
                    await botData.LoadAsync(CancellationToken.None);

                    IDialogTask task = scope.Resolve<IDialogTask>();

                    QuestionDialog dialog = new QuestionDialog();
                    task.Call(dialog.Void<object, IMessageActivity>(), null);

                    await task.PollAsync(CancellationToken.None);

                    //flush dialog stack
                    await botData.FlushAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            { }
        }
    }

    public class ConversationHistory : TableEntity
    {
        public ConversationHistory()
        { }

        public ConversationHistory(IMessageActivity message)
        {
            this.PartitionKey = message.ChannelId;
            this.RowKey = message.Conversation.Id;
            this.Timestamp = DateTime.UtcNow;
            ConversationReference conversationReference = message.ToConversationReference();
            this.Conversation = JsonConvert.SerializeObject(conversationReference);
        }

        public string Conversation { get; set; }
    }
}