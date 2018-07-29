using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Parser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var errors = new List<string>();
            var queue = new Queue<string>(GetUrls());

            IQuestionData questionData = new QuestionDataSql(true);

            using (HttpClient client = new HttpClient())
            {
                while (queue.Count > 0)
                {
                    string link = queue.Dequeue();
                    Trace.WriteLine("");
                    Trace.TraceInformation($"Start processing {link}");
                    Trace.WriteLine("");
                    string[] newQ = await SendRequest(client, link, questionData);

                    foreach (string item in newQ)
                    {
                        if (!queue.Contains(item))
                        {
                            queue.Enqueue(item);
                        }
                    }

                    Trace.WriteLine("");
                    Trace.TraceInformation($"End processing {link}");
                    Trace.WriteLine("");
                    Trace.WriteLine("******************************");
                    Trace.WriteLine("");
                }
                //for (int i = 0; i < links.Count; i++)
                //{
                //    string link = links[i];
                //    Trace.WriteLine("");
                //    Trace.TraceInformation($"{i + 1}. Start processing {link}");
                //    Trace.WriteLine("");
                //    SendRequest(client, link, questionData).Wait();
                //    Trace.WriteLine("");
                //    Trace.TraceInformation($"{i + 1}. End processing {link}");
                //    Trace.WriteLine("");
                //    Trace.WriteLine("******************************");
                //    Trace.WriteLine("");
                //}
            }

            Console.ReadLine();
        }

        private static string[] GetUrls()
        {
            //return System.IO.File.ReadAllLines(@"D:\urls.txt");
            return new[] { "/tour/AUTHORS" };
        }

        private static async Task<string[]> SendRequest(HttpClient client, string url, IQuestionData questionData)
        {
            string xml = "";
            string json = null;
            JObject token = null;

            try
            {
                xml = await client.GetStringAsync($"https://db.chgk.info{url}/xml");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when get request", ex);
                return new string[] { };
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when loading text to XmlDocument", ex);
                return new string[] { };
            }

            try
            {
                json = JsonConvert.SerializeXmlNode(doc.LastChild, Newtonsoft.Json.Formatting.None, true);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when Serializing to XmlNode", ex);
                return new string[] { };
            }

            try
            {
                token = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when parsing JToken", ex);
                return new string[] { };
            }

            try
            {
                var tours = token["tour"];
                var question = token["question"];
                if (tours != null)
                {
                    var tour = token.ToObject<Tour>();
                    questionData.InsertTour(tour);
                    var childs = (tours as JArray).Select(x => $"/tour/{x["TextId"]}").ToArray();
                    return childs;
                }
                else if (question != null)
                {
                    var tournament = token.ToObject<Tournament>();
                    questionData.InsertTournament(tournament);
                }
                else
                {
                    var tt = 3;
                }
                return new string[] { };
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when inserting to DB", ex);
                return new string[] { };
            }
        }
    }
}
