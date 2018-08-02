using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class TourDialog : IDialog<object>
    {
        private IQuestionData GetData() => new QuestionDataSql();

        private const string ID_REGEX = "^#(\\d+)";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity message = await result;

            string text = message.Text.Trim().ToLower();

            if (text.Contains("finish"))
            {
                var reply = context.MakeMessage();
                reply.Text = "I'm done";
                context.Done(reply);
            }
            else if (text.Contains("tours"))
            {
                var tours = await GetData().GetTours();
                if (tours.Any())
                {
                    PromptDialog.Choice(context, ResumeAfterSelectingTour, tours.Select(x => x.ToString()), "Please select a tour:", descriptions: tours.Select(x => x.Title));
                }
                else
                {
                    await context.SayAsync("I'm sorry I cannot find tours.");
                    context.Done(message);
                }
            }
            else
            {
                await context.SayAsync("TourDialog: " + text);
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task ResumeAfterSelectingTour(IDialogContext context, IAwaitable<string> result)
        {
            string tourTitle = await result;
            var match = Regex.Match(tourTitle, ID_REGEX);
            if (match.Success)
            {
                int tourId = int.Parse(match.Groups[1].Value);
                var tourBase = await GetData().GetTourById(tourId);

                var tour = tourBase as Tour;
                var tournament = tourBase as Tournament;
                if (tournament != null)
                {
                    PromptDialog.Choice(context, 
                        ResumeAfterSelectingTour, 
                        tournament.Tours.Select(x => x.ToString()), 
                        "Please select a tour:", 
                        descriptions: tournament.Tours.Select(x => x.Title));
                }
                else if (tour != null)
                {
                    PromptDialog.Choice(context, ResumeAfterSelectingQuestion,
                        tour.Questions.Select(x => x.ToString()),
                        "Please select a question:",
                        descriptions: tour.Questions.Select(x => x.Question.Substring(0, Math.Min(15, x.Question.Length)) + "..."));
                }
                else
                {
                    await context.PostAsync($"Tour {tourTitle} is not found.");
                }
            }
            else
            {
                await context.PostAsync($"Tour {tourTitle} is not found.");
            }
        }

        private async Task ResumeAfterSelectingQuestion(IDialogContext context, IAwaitable<string> result)
        {
            string questionTitle = await result;
            var match = Regex.Match(questionTitle, ID_REGEX);
            if (match.Success)
            {
                int questionId = int.Parse(match.Groups[1].Value);
                await context.PostAsync($"Question {questionTitle} is selected." + questionId);
            }
            else
            {
                await context.PostAsync($"Question {questionTitle} is not found.");
            }
        }
    }

}