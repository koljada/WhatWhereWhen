using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly string NL = Environment.NewLine;

        [NonSerialized]
        private readonly IQuestionData _questionData;

        public QuestionDialog()
        {
            _questionData = new QuestionDataSql();
        }        

        public async Task StartAsync(IDialogContext context)
        {
            await PostNewQuestion(context);

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

                reply.ReplyToId = q.Id.ToString();

                CardAction answerAction = new CardAction()
                {
                    Text = q.Answer,
                    Title = "New Question",
                    Type = "imBack",
                    Value = "new"
                };

                string answerText = "Question:" + "\n\n<br/>" + q.Question + "\n\n<br/><br/><hr/>" +
                    "Answer:" + "\n\n<br/>" + q.Answer + "\n\n<br/><br/><hr/>" +
                    "Sourse:" + "\n\n<br/>" + q.Sources;

                HeroCard card = new HeroCard
                {
                    Title = "Answer",
                    Text = answerText,
                    Images = q.AnswerImageUrls.Select(url => new CardImage(url)).ToList(),
                    Subtitle = $"Rating: {q.RatingNumber}{NL}{NL}<br/> Level: {q.Complexity}",
                    Tap = answerAction,
                    Buttons = new List<CardAction>() { answerAction }
                };

                reply.Attachments = new List<Attachment> { card.ToAttachment() };

                await context.PostAsync(reply);               
            }
            else if (text.Contains("new"))
            {
                context.Call(new QuestionDialog(), Finish);
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
        
        private async Task PostNewQuestion(IDialogContext context)
        {
            string conversationId = context.MakeMessage().Conversation.Id;

            byte level = context.ConversationData.GetValueOrDefault<byte>(KEY_LEVEL);

            QuestionItem newQuestion = await _questionData.GetRandomQuestion(conversationId, (QuestionComplexity)level);

            if (newQuestion != null)
            {
                newQuestion.InitUrls("https://db.chgk.info/images/db/");

                context.ConversationData.SetValue(KEY, newQuestion);

                IMessageActivity message = context.MakeMessage();

                message.Id = newQuestion.Id.ToString();

                CardAction answerAction = new CardAction()
                {
                    Text = newQuestion.Answer,
                    Title = "Answer",
                    Type = "imBack",
                    Value = "answer"
                };

                HeroCard card = new HeroCard
                {
                    Title = "New question",
                    Text = newQuestion.Question,
                    Images = newQuestion.QuestionImageUrls.Select(url => new CardImage(url)).ToList(),
                    Subtitle = $"Author: {newQuestion.Authors}. {NL}{NL}<br/> Tour: {newQuestion.TournamentTitle}",
                    Tap = answerAction,
                    Buttons = new List<CardAction>() { answerAction }
                };

                message.Attachments = new List<Attachment> { card.ToAttachment() };

                await context.PostAsync(message);
            }
        }
    }
}