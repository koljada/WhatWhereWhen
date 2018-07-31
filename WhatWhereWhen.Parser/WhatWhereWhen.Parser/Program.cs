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

            QuestionDataSql questionData = new QuestionDataSql(true);

            using (HttpClient client = new HttpClient())
            {
                while (queue.Count > 0)
                {
                    string link = queue.Dequeue();
                    //Trace.WriteLine("");
                    //Trace.TraceInformation($"Start processing {link}");
                    //Trace.WriteLine("");
                    string[] newQ = await SendRequest(client, link, questionData);

                    foreach (string item in newQ)
                    {
                        if (!queue.Contains(item))
                        {
                            queue.Enqueue(item);
                        }
                    }

                    //Trace.WriteLine("");
                    //Trace.TraceInformation($"End processing {link}");
                    //Trace.WriteLine("");
                    //Trace.WriteLine("******************************");
                    //Trace.WriteLine("");
                }                
            }

            Console.ReadLine();
        }

        private static string[] GetUrls()
        {
            //return System.IO.File.ReadAllLines(@"D:\urls.txt");
            return new[] { "/tour/AUTHORS", "/tour/INTER", "/tour/SINHR", "/tour/NEPOLN", "/tour/REGION", "/tour/INET", "/tour/R100", "/tour/TELE", "/tour/TREN", "/tour/TEMA", "/tour/ERUDITK", "/tour/EF", "/tour/BESKR", "/tour/SVOYAK" };
        }

        private static async Task<string[]> SendRequest(HttpClient client, string url, QuestionDataSql questionData)
        {
            string xml = "";
            string json = null;
            JObject token = null;
            string fullUrl = $"https://db.chgk.info{url}/xml";

            try
            {
                xml = await client.GetStringAsync(fullUrl);
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
                if (token.Value<int?>("Complexity") > 256)
                {
                    token["Complexity"] = null;
                }
                if (token.Value<string>("PlayedAt") == "0000-00-00")
                {
                    token["PlayedAt"] = null;
                }
                int id = token.Value<int>("Id");
                if (tours != null)
                {
                    List<string> result = new List<string>();

                    if (!(tours is JArray toursArray))
                    {
                        toursArray = new JArray { tours };
                        token["tour"] = toursArray;
                    }

                    foreach (var ch in toursArray)
                    {
                        var textId = ch["TextId"].ToString();
                        if (!string.IsNullOrEmpty(textId))
                        {
                            result.Add($"/tour/{textId}");
                        }

                        //if (ch.Value<int?>("Complexity") > 256)
                        //{
                        //    ch["Complexity"] = null;
                        //}
                        //if (ch.Value<string>("PlayedAt") == "0000-00-00")
                        //{
                        //    ch["PlayedAt"] = null;
                        //}
                    }

                    //var tour = token.ToObject<Tour>();
                    //if (string.IsNullOrWhiteSpace(tour.URL))
                    //{
                    //    tour.URL = fullUrl;
                    //}
                    //questionData.InsertTour(tour);

                    return result.ToArray();
                }
                else if (question != null)
                {
                    //if (!(question is JArray questionsArray))
                    //{
                    //    questionsArray = new JArray { question };
                    //    token["question"] = questionsArray;
                    //}

                    //var tournament = token.ToObject<Tournament>();
                    //if (string.IsNullOrWhiteSpace(tournament.URL))
                    //{
                    //    tournament.URL = fullUrl;
                    //}

                    //questionData.InsertTournament(tournament);
                    questionData.UpdateUrl(id, fullUrl);
                }
                
                return new string[] { };
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception when inserting to DB, {ex.Message}", ex);
                return new string[] { };
            }
        }
    }
}
