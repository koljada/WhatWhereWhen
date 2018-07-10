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
        private const string KEY_LEVEL = "CURRENT_LEVEL";

        private readonly bool _withTitle = true;
        private readonly string NL = Environment.NewLine;

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
            await PostNewQuestion(context, _withTitle ? "Here is today's question: " : "");

            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity message = await argument;

            string text = message.Text.Trim().ToLower();

            if (text.Contains("answer"))
            {
                QuestionItem q = context.ConversationData.GetValue<QuestionItem>(KEY);

                IMessageActivity reply = context.MakeMessage();

                reply.Attachments = GetAttachments(q.Answer, "Answer");

                q.Answer = Regex.Replace(q.Answer, "\\(pic: (.*)\\)", "");

                reply.Text = "> " + q.Question + NL + NL +
                                    "Answer: " + NL + q.Answer + NL + NL +
                                    "Source: " + NL + q.Sources;

                reply.ReplyToId = q.Id.ToString();

                await context.PostAsync(reply);

                if (!text.Contains("-off"))
                {
                    context.Call(new QuestionDialog(false), Finish);
                }
            }
            else if (text.Contains("new"))
            {
                context.Call(new QuestionDialog(false), Finish);
            }
            else if (text.Contains("level "))
            {
                var match = Regex.Match(text, "level(\\s)+([0-5])");
                if (match.Success)
                {
                    string value = match.Groups[2].Value;
                    context.ConversationData.SetValue(KEY_LEVEL, byte.Parse(value));

                    await context.PostAsync("Level is set up to " + value);
                }
                else await context.PostAsync("Level must be within [0-5] range");
            }
            else
            {
                await context.PostAsync("Sorry, I did not get it." + NL + NL + Constants.HELP);

                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task Finish(IDialogContext context, IAwaitable<object> result) => context.Done(await result);

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

            byte level = context.ConversationData.GetValueOrDefault<byte>(KEY_LEVEL);

            QuestionItem newQuestion = await _questionData.GetRandomQuestion(conversationId, (QuestionComplexity)level);

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