using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string KEY = "CURRENT_QUESTION";
        private const string KEY_LEVEL = "CURRENT_LEVEL";
        private const string TIMER = "TIMER_CANCELLATION";
       
        private readonly string NL = Environment.NewLine;

        private static IQuestionData GetData() => new QuestionDataSql();

        public async Task StartAsync(IDialogContext context)
        {
            Trace.TraceInformation($"RootDialog.StartAsync");
            context.Wait(MessageReceivedAsync);
        }
       
        private bool IsTimerFeatureEnabled() => Convert.ToBoolean(ConfigurationManager.AppSettings["TimerEnabled"]);

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity activity = await result;

            ConversationStarter.SaveConversation(activity);

            string text = activity.Text.ToLower().Trim();

            context.ConversationData.SetValue(TIMER, false);//cancel timer            

            //string text2 = activity.RemoveRecipientMention();

            if (text.EndsWith("answer"))
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: answer case.");

                QuestionItem q = context.ConversationData.GetValueOrDefault<QuestionItem>(KEY);

                if (q != null)
                {
                    IMessageActivity reply = context.MakeMessage();

                    reply.ReplyToId = q.Id.ToString();

                    CardAction questionAction = new CardAction
                    {
                        Title = "New question",
                        Type = "imBack",
                        Value = "new"
                    };

                    reply.Text = $"**Question:**<br/>{q.Question}<br/>**Answer:**<br/>{q.Answer}";

                    if (!string.IsNullOrWhiteSpace(q.Rating))
                    {
                        reply.Text += $"<br/><br/>**Rating: **{q.Rating}";
                    }
                    if (q.Complexity.HasValue)
                    {
                        reply.Text += $"<br/>**Level:** {q.Complexity}";
                    }

                    if (!string.IsNullOrWhiteSpace(q.Comments))
                    {
                        reply.Text += $"<br/>**Comments:**<br/>{q.Comments}";
                    }
                    if (!string.IsNullOrWhiteSpace(q.Sources))
                    {
                        reply.Text += $"<br/>**Source:**<br/>{q.Sources}";
                    }

                    reply.Attachments = q.AnswerImageUrls.Select(ToAttachement).ToList();

                    reply.SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction> { questionAction }
                    };

                    if (IsTimerFeatureEnabled())
                    {
                        reply.SuggestedActions.Actions.Add(new CardAction
                        {
                            Title = "New question with 1 min timer",
                            Type = "imBack",
                            Value = "new 1 min"
                        });

                        reply.SuggestedActions.Actions.Add(new CardAction
                        {
                            Title = "New question with 2 min timer",
                            Type = "imBack",
                            Value = "new 2 min"
                        });
                    }

                    await context.PostAsync(reply);
                }
                else await context.SayAsync("Sorry, I can't find the question to answer.");
            }
            else if (text.Contains("new"))
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: new case.");
                var newQuestion = await PostNewQuestion(context.ConversationData, context.MakeMessage());
                if (newQuestion != null)
                {
                    await context.PostAsync(newQuestion);
                }
                else
                {
                    Trace.TraceError("NULL in PostNewQuestion");
                    await context.SayAsync("I'm sorry - something wrong has happened.");
                }

                if (IsTimerFeatureEnabled())
                {
                    Match match = Regex.Match(text, "new(\\s)+([1-5])(\\s)?min");
                    if (match.Success)
                    {
                        string value = match.Groups[2].Value;
                        int minutes = int.Parse(value);
                        context.ConversationData.SetValue(TIMER, true);

                        Task timerTask = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromMinutes(minutes));

                            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                IBotData botData = scope.Resolve<IBotData>();
                                await botData.LoadAsync(CancellationToken.None);

                                if (botData.ConversationData.GetValueOrDefault<bool>(TIMER))
                                {
                                    context.ConversationData.SetValue(TIMER, false);
                                    await context.PostAsync("The time is over. (time)");
                                    await botData.FlushAsync(CancellationToken.None);
                                }
                            }
                        });

                        await context.SayAsync("(time)");
                    }
                }
            }
            else if (text.Contains("level"))
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: level case.");

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
                    Dictionary<byte, string> options = Enumerable.Range(0, 6).ToDictionary(x => (byte)x, x => x == 0 ? "Random" : x.ToString());
                    PromptDialog.Choice(context, ResumeAfterSelectLevel, options.Keys, "Please select a level", descriptions: options.Values);
                    return;
                };

            }
            else if (text.Contains("clear conversation data"))
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: clear conversation data case.");
                context.ConversationData.Clear();
            }
            else if (text.Contains("help"))
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: help case.");
                await context.PostAsync("HELP");
            }
            else if (text.Contains("tours"))
            {
                await context.Forward(new TourDialog(), ResumeAfterNewOrderDialog, activity, CancellationToken.None);
                return;

            }
            else
            {
                Trace.TraceInformation("RootDialog.MessageReceivedAsync: other case.");
                if (context.ConversationData.GetValueOrDefault<bool>("init2"))//must not be executed on the first run
                {
                    await context.PostAsync("Sorry, I did not get it." + NL + NL + "HELP");
                }
                else
                {
                    context.ConversationData.SetValue("init2", true);
                }
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterSelectLevel(IDialogContext context, IAwaitable<byte> result)
        {
            byte level = await result;
            context.ConversationData.SetValue(KEY_LEVEL, level);
            await context.SayAsync($"Level is set to {(level == 0 ? "random" : level.ToString())}");
        }

        private async Task ResumeAfterNewOrderDialog(IDialogContext context, IAwaitable<object> result)
        {
            // Store the value that NewOrderDialog returned. 
            // (At this point, new order dialog has finished and returned some value to use within the root dialog.)
            IMessageActivity resultStr = await result as IMessageActivity;

            await context.PostAsync($"Tour dialog just told me this: {resultStr.Text}");

            // Again, wait for the next message from the user.
            context.Wait(this.MessageReceivedAsync);
        }

        public async static Task<IMessageActivity> PostNewQuestion(IBotDataBag conversationData, IMessageActivity messageActivity)
        {
            string conversationId = messageActivity.Conversation.Id;

            byte level = conversationData.GetValueOrDefault<byte>(KEY_LEVEL);

            Trace.TraceInformation($"PostNewQuestion: conversation: {conversationId}; level: {level}");

            QuestionItem newQuestion = await new QuestionDataSql().GetRandomQuestion(conversationId, (QuestionComplexity)level);

            Trace.TraceInformation($"PostNewQuestion: {newQuestion?.Id}, {newQuestion?.Question}, {newQuestion?.Answer}");

            if (newQuestion != null)
            {
                newQuestion.InitUrls("https://db.chgk.info/images/db/");

                conversationData.SetValue(KEY, newQuestion);

                messageActivity.Id = newQuestion.Id.ToString();

                CardAction answerAction = new CardAction()
                {
                    Title = "Answer",
                    Type = "imBack",
                    Value = "answer"
                };

                messageActivity.Text = $"**Question:**<br/>{newQuestion.Question}<br/><br/>" +
                    $"**Author:** {newQuestion.Authors ?? "Unknkown"}<br/>" +
                    $"**Tour:** {newQuestion.TournamentTitle ?? "Unknkown"}";

                messageActivity.Attachments = newQuestion.QuestionImageUrls.Select(ToAttachement).ToList();
                messageActivity.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction> { answerAction }
                };

                return messageActivity;
            }

            return null;
        }

        private static Attachment ToAttachement(string url)
        {
            string ext = url.Split('.').Last();
            return new Attachment
            {
                ContentType = "image/" + ext,
                ContentUrl = url,
                Name = "Picture"
            };
        }
    }
}