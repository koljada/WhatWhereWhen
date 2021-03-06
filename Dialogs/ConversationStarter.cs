﻿using System;
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
using System.Diagnostics;

using Activity = Microsoft.Bot.Connector.Activity;

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
            Trace.TraceInformation($"SaveConversation { message.Conversation.Id}, { message.Conversation.Name}, { message.Conversation.IsGroup}; " +
                $"from: {message.From.Id}, {message.From.Name}; type: {message.Type}; channel: {message.ChannelId}");

            try
            {
                CloudTable table = GetTable();

                ConversationHistory conversation = new ConversationHistory(message);

                TableOperation insertOperation = TableOperation.InsertOrReplace(conversation);
                table.Execute(insertOperation);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when saving conversation", ex);
            }
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
                Trace.TraceError("Exception when fetching conversations", ex);
                return new List<ConversationHistory>();
            }
        }

        public static async Task Resume()
        {
            var conversations = GetConversations();

            Trace.TraceInformation($"Resuming  {conversations.Count} conversations");

            foreach (ConversationHistory history in conversations)
            {
                await Resume(history);
            }
        }

        public static async Task Resume(ConversationHistory history)
        {
            Activity message = null;
            Trace.TraceInformation($"Resuming  {history.PartitionKey} {history.PartitionKey}");
            try
            {
                message = JsonConvert.DeserializeObject<ConversationReference>(history.Conversation).GetPostToBotMessage();
                ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

                using (ILifetimeScope scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    IBotData botData = scope.Resolve<IBotData>();

                    await botData.LoadAsync(CancellationToken.None);

                    string todayDate = DateTime.UtcNow.ToShortDateString();
                    if (botData.ConversationData.GetValueOrDefault<string>("today") != todayDate)
                    {
                        botData.ConversationData.SetValue("today", todayDate);
                        await botData.FlushAsync(CancellationToken.None);
                        await botData.LoadAsync(CancellationToken.None);

                        IMessageActivity temp = message.CreateReply().AsMessageActivity();

                        IMessageActivity reply = await RootDialog.PostNewQuestion(botData.ConversationData, temp);

                        if (reply != null)
                        {
                            await client.Conversations.SendToConversationAsync((Activity)reply);
                        }

                        //flush dialog stack
                        await botData.FlushAsync(CancellationToken.None);
                    }                    
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception when resuming conversation {message.Conversation?.Id}", ex);
            }
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