﻿using Microsoft.Bot.Builder.Dialogs;
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

                CardAction questionAction = new CardAction()
                {
                    Title = "New question",
                    Type = "imBack",
                    Value = "new"
                };

                reply.Text = $"**Question:**<br/>{q.Question}<br/>**Answer:**<br/>{q.Answer}<br/><br/>" +
                    $"**Source:**<br/>{q.Sources}<br/><br/>**Rating:** {q.Rating}<br/><br/>**Level:** {q.Complexity}";

                reply.Attachments = q.AnswerImageUrls.Select(x => new Attachment() { ContentUrl = x }).ToList();

                reply.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction> { questionAction }
                };

                await context.PostAsync(reply);
            }
            else if (text.Contains("new"))
            {
                context.Call(new QuestionDialog(), Finish);
            }
            else if (text.Contains("level"))
            {
                var match = Regex.Match(text, "level(\\s)+([0-5])");
                if (match.Success)
                {
                    string value = match.Groups[2].Value;
                    context.ConversationData.SetValue(KEY_LEVEL, byte.Parse(value));

                    await context.PostAsync("Level is set up to " + value);
                }
                else
                {
                    IMessageActivity reply = context.MakeMessage();
                    List<CardAction> actions = Enumerable.Range(1, 5).Select(x => new CardAction
                    {
                        Title = x.ToString(),
                        Type = "imBack",
                        Value = "level " + x
                    }).ToList();

                    actions.Insert(0, new CardAction("imBack", "Random", value: "level 0"));
                    reply.Text = "Please select a level";
                    reply.SuggestedActions = new SuggestedActions(null, actions);

                    await context.PostAsync(reply);
                };
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
                    Title = "Answer",
                    Type = "imBack",
                    Value = "answer"
                };

                message.Text = $"**Question:**<br/>{newQuestion.Question}<br/><br/>" +
                    $"**Author:** {newQuestion.Authors}<br/><br/>" +
                    $"**Tour:** {newQuestion.TournamentTitle}";

                message.Attachments = newQuestion.QuestionImageUrls.Select(x => new Attachment() { ContentUrl = x }).ToList();

                message.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction> { answerAction }
                };

                await context.PostAsync(message);
            }
        }
    }
}