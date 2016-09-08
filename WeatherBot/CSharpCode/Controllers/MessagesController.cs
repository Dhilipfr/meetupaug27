using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web.Http.Tracing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bot_Application1
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                // calculate something for us to return
                //int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                //Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                //await connector.Conversations.ReplyToActivityAsync(reply);

                var message = activity;
                var location = await QueryLuisWeatherLocation(message.Text);

                Activity replyMessage = message.CreateReply("Hey there! Wondering if it's raining outside? I can tell you. But you gotta ask me, nicely.");
                if (location != null)
                {
                    var weatherMessage = await GetCurrentWeather(location);
                    if (weatherMessage != null)
                    {
                        replyMessage = message.CreateReply(weatherMessage);
                    }
                }

                if (replyMessage == null)
                {
                    replyMessage = message.CreateReply("Sorry, I don't understand.");
                }

                await connector.Conversations.ReplyToActivityAsync(replyMessage);
                //return Request.CreateResponse(HttpStatusCode.OK, replyMessage);
                Console.WriteLine(location);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
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
                return message.CreateReply("Go on! Let's talk :) ");
            }
            else if (message.Type == ActivityTypes.Ping)
            {
                return message.CreateReply("I'm awake!");
            }

            return null;
        }

        private static async Task<string> QueryLuisWeatherLocation(string message)
        {
            using (var client = new HttpClient())
            {
                dynamic response = JObject.Parse(await client.GetStringAsync(@"https://api.projectoxford.ai/luis/v1/application?id=c413b2ef-382c-45bd-8ff0-f76d60e2a821&subscription-key=a50372faace845b58c14ea53b56a7217&q="
                    + System.Uri.EscapeDataString(message)));

                var intent = response.intents?.First?.intent;

                if (intent == "builtin.intent.weather.check_weather")
                {
                    var entity = response.entities?.First;
                    if (entity?.type == "builtin.weather.absolute_location")
                    {
                        return entity.entity;
                    }
                }

                return null;
            }
        }

        private static async Task<string> GetCurrentWeather(string location)
        {
            using (var client = new HttpClient())
            {
                var escapedLocation = Regex.Replace(location, @"\W+", "_");

                dynamic response = JObject.Parse(await client.GetStringAsync($"http://api.wunderground.com/api/8f30d93dffa4aaff/conditions/q/{escapedLocation}.json"));

                dynamic observation = response.current_observation;
                dynamic results = response.response.results;

                if (observation != null)
                {
                    string displayLocation = observation.display_location?.full;
                    decimal tempC = observation.temp_c;
                    string weather = observation.weather;

                    return $"It is {weather} and {tempC} degrees in {displayLocation}.";
                }
                else if (results != null)
                {
                    return $"There is more than one '{location}'. Can you be more specific?";
                }

                return null;
            }
        }



    }
}