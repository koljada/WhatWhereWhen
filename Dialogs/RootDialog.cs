using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.Bot.Sample.SimpleEchoBot.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string KEY_ID = "QuestionId";
        private const string KEY_ANSWER = "Answer";
        private const string KEY_SOURCE = "Source";

        public async Task StartAsync(IDialogContext context)
        {
            try
            {
                int qusetionId = context.ConversationData.GetValueOrDefault<int>(KEY_ID);
                if (qusetionId == 0)
                {
                    await context.PostAsync("Hello, I'll post some random question every morning.");
                }

                await PostNewQuestion(context, "Hi all! Here is the today question: ");
            }
            catch (Exception ex)
            {
                await context.PostAsync(ex.Message);
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity message = await argument;

            ConversationStarter.CheckReference(message);

            string text = message.Text.Trim().ToLower();

            if (text == "answer")
            {
                string answer = context.ConversationData.GetValue<string>(KEY_ANSWER);
                string source = context.ConversationData.GetValue<string>(KEY_SOURCE);
                await context.PostAsync("Answer: " + Environment.NewLine + answer + Environment.NewLine + source);
            }
            else if (text == "question")
            {
                await PostNewQuestion(context, "Question: " + Environment.NewLine);
            }
            else if (text == "help")
            {
                await context.PostAsync("Commands: " + Environment.NewLine +
                    "\t\t  - type `question` to get a new question;" + Environment.NewLine +
                    "\t\t  - type `answer` to get an answer to the current question;" + Environment.NewLine +
                    "\t\t  - type `help` to get the help;"
                    );
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task PostNewQuestion(IDialogContext context, string text)
        {
            context.ConversationData.RemoveValue(KEY_ID);
            context.ConversationData.RemoveValue(KEY_ANSWER);

            QuestionItem newQuestion = GetQuestion();
            if (newQuestion != null)
            {
                context.ConversationData.SetValue(KEY_ID, newQuestion.QuestionId);
                context.ConversationData.SetValue(KEY_ANSWER, newQuestion.Answer);
                context.ConversationData.SetValue(KEY_SOURCE, newQuestion.Sources);

                await context.PostAsync(text +
                    Environment.NewLine + newQuestion.Question);
            }
        }

        private QuestionItem GetQuestion()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("Questions");
            table.CreateIfNotExists();

            var question = table.CreateQuery<QuestionItem>()
                .Where(x => x.PartitionKey == "False")
                .Take(1)
                .ToList()
                .FirstOrDefault();

            if (question != null)
            {
                var copy = question.Copy();

                TableOperation insertOperation = TableOperation.Insert(copy);
                table.Execute(insertOperation);

                TableOperation deleteOperation = TableOperation.Delete(question);
                table.Execute(deleteOperation);
            }

            return question;
        }
    }
}