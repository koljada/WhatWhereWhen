using System.Threading.Tasks;
using System.Web.Http;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System;
using SimpleEchoBot.Dialogs;
using System.Diagnostics;

using Activity = Microsoft.Bot.Connector.Activity;
using WhatWhereWhen.Data.Sql;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [BotAuthentication]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return Ok();
        }

        [AllowAnonymous]
        public async Task<IHttpActionResult> Put()
        {
            try
            {
                Trace.TraceInformation($"PUT request. Starting resuming conversations");

                await ConversationStarter.Resume();

                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceError("PUT: Exception when resuming", ex);

                return BadRequest(ex.ToString());
            }
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                Trace.TraceInformation("Conversation update");
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}