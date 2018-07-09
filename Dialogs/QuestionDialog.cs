using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Microsoft.Bot.Sample.SimpleEchoBot.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class QuestionDialog : IDialog<object>
    {
        private const string KEY_ID = "QuestionId";
        private const string KEY_QUESTION = "Question";
        private const string KEY_ANSWER = "Answer";
        private const string KEY_ANSWER_PICTURE_URL = "AnswerPicureUrl";
        private const string KEY_SOURCE = "Source";

        public async Task StartAsync(IDialogContext context)
        {
            try
            {
                await PostNewQuestion(context, "Here is today's question: ");
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

            string text = message.Text.Trim().ToLower();

            if (text == "answer" || text == "a")
            {
                string answer = context.ConversationData.GetValue<string>(KEY_ANSWER);
                string source = context.ConversationData.GetValue<string>(KEY_SOURCE);
                string question = context.ConversationData.GetValue<string>(KEY_QUESTION);
                long questionId = context.ConversationData.GetValue<long>(KEY_ID);

                IMessageActivity reply = context.MakeMessage();

                string answerUrl = null;
                if (context.ConversationData.TryGetValue(KEY_ANSWER_PICTURE_URL, out answerUrl))
                {
                    reply.Attachments = new List<Attachment> { new Attachment("image/png", answerUrl) };
                }

                reply.Text = "> " + question + Environment.NewLine + Environment.NewLine + 
                                    "Answer: " + Environment.NewLine + answer;

                reply.ReplyToId = questionId.ToString();

                await context.PostAsync(reply);

                context.Call(new QuestionDialog(), Finish);
            }
            else if (text == "question" || text == "q")
            {
                context.Call(new QuestionDialog(), Finish);
            }
            else if (text == "help")
            {
                await context.PostAsync("Commands: " + Environment.NewLine +
                    "\t\t  - type `question` or `q` to get a new question;" + Environment.NewLine +
                    "\t\t  - type `answer` or `a` to get an answer to the current question;" + Environment.NewLine);

                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task Finish(IDialogContext context, IAwaitable<object> result)
        {
            object t = await result;
            context.Done(t);
        }

        private async Task PostNewQuestion(IDialogContext context, string text)
        {
            QuestionItem newQuestion = GetQuestion();
            if (newQuestion != null)
            {
                context.ConversationData.SetValue(KEY_ID, newQuestion.QuestionId);
                context.ConversationData.SetValue(KEY_QUESTION, newQuestion.Question);
                context.ConversationData.SetValue(KEY_ANSWER, newQuestion.Answer);
                if (newQuestion.AnswerPictureUrl != null)
                {
                    context.ConversationData.SetValue(KEY_ANSWER_PICTURE_URL, newQuestion.AnswerPictureUrl);
                }
                context.ConversationData.SetValue(KEY_SOURCE, newQuestion.Sources);

                var message = context.MakeMessage();
                message.Id = newQuestion.QuestionId.ToString();
                if (newQuestion.QuestonPictureUrl != null)
                {
                    message.Attachments = new List<Attachment>
                    {
                        new Attachment("image/png", newQuestion.QuestonPictureUrl, null)
                    };
                }
                message.Text = text + Environment.NewLine + newQuestion.Question;

                await context.PostAsync(message);
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