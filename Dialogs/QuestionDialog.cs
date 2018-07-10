using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class QuestionDialog : IDialog<object>
    {
        private const string KEY = "CURRENT_QUESTION";

        private readonly bool _withTitle = true;

        [NonSerialized]
        private readonly IQuestionData _questionData;

        public QuestionDialog()
        {
            _questionData = new QuestionDataSql();
        }

        public QuestionDialog(bool withTitle) : this()
        {
            _withTitle = withTitle;
        }

        public async Task StartAsync(IDialogContext context)
        {
            try
            {
                await PostNewQuestion(context, _withTitle ? "Here is today's question: " : "");
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

            if (text.EndsWith("answer") || text.EndsWith("-a"))
            {
                QuestionItem q = context.ConversationData.GetValue<QuestionItem>(KEY);

                IMessageActivity reply = context.MakeMessage();

                reply.Attachments = GetAttachments(q.Answer, "Answer");

                q.Answer = Regex.Replace(q.Answer, "\\(pic: (.*)\\)", "");

                reply.Text = "> " + q.Question + Environment.NewLine + Environment.NewLine +
                                    "Answer: " + Environment.NewLine + q.Answer;

                reply.ReplyToId = q.Id.ToString();

                await context.PostAsync(reply);

                context.Call(new QuestionDialog(false), Finish);
            }
            else if (text.EndsWith("question") || text.EndsWith("-q"))
            {
                context.Call(new QuestionDialog(false), Finish);
            }
            else if (text.EndsWith("help"))
            {
                await context.PostAsync("Commands: " + Environment.NewLine +
                    "\t\t  - type `question` to get a new question;" + Environment.NewLine +
                    "\t\t  - type `answer` to get an answer to the current question;" + Environment.NewLine);

                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task Finish(IDialogContext context, IAwaitable<object> result)
        {
            object t = await result;
            context.Done(t);
        }

        private IList<Attachment> GetAttachments(string text, string name)
        {
            var result = new List<Attachment>();
            string regexpPattern = "\\(pic: (.*)\\)";
            string baseUrl = "https://db.chgk.info/images/db/";

            var mathces = Regex.Matches(text, regexpPattern);

            foreach (Match match in mathces)
            {
                string url = match.Groups[1].Value;
                string ext = url.Split('.')[1];
                result.Add(new Attachment("image/" + ext, baseUrl + url, null, name));
            }

            return result;
        }

        private async Task PostNewQuestion(IDialogContext context, string text)
        {
            string conversationId = context.MakeMessage().Conversation.Id;

            QuestionItem newQuestion = await _questionData.GetRandomQuestion(conversationId);

            if (newQuestion != null)
            {
                context.ConversationData.SetValue(KEY, newQuestion);

                IMessageActivity message = context.MakeMessage();

                message.Id = newQuestion.Id.ToString();

                message.Attachments = GetAttachments(newQuestion.Question, "Question");

                newQuestion.Question = Regex.Replace(newQuestion.Question, "\\(pic: (.*)\\)", "");

                message.Text = text + Environment.NewLine + newQuestion.Question;

                await context.PostAsync(message);
            }
        }
    }
}