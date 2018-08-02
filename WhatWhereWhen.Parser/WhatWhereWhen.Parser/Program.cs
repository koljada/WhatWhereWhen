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
                    await SendRequest(client, link, questionData);

                    //foreach (string item in newQ)
                    //{
                    //    if (!queue.Contains(item))
                    //    {
                    //        queue.Enqueue(item);
                    //    }
                    //}

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
            return new[] {
                //"/tour/AUTHORS", "/tour/INTER", "/tour/SINHR", "/tour/NEPOLN", "/tour/REGION", "/tour/INET",
                //"/tour/R100", "/tour/TELE", "/tour/TREN", "/tour/TEMA", "/tour/ERUDITK", "/tour/EF", "/tour/BESKR",
                "/tour/stre16br","/tour/univ16br" };
            //return new[] { "/tour/AUTHORS", "/tour/INTER", "/tour/SINHR", "/tour/NEPOLN", "/tour/REGION", "/tour/INET", "/tour/R100", "/tour/TELE", "/tour/TREN", "/tour/TEMA", "/tour/ERUDITK", "/tour/EF", "/tour/BESKR", "/tour/SVOYAK" };
        }

        private static JObject ParseString(string xml)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when loading text to XmlDocument", ex);
                return null;
            }

            try
            {
                string json = JsonConvert.SerializeXmlNode(doc.LastChild, Newtonsoft.Json.Formatting.None, true);
                try
                {
                    return JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception when parsing JToken", ex);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when Serializing to XmlNode", ex);
                return null;
            }
        }

        private static void FixSchemaErrors(string url, ref JObject token)
        {
            if (token.Value<int?>("Complexity") > 256)
            {
                token["Complexity"] = null;
            }
            if (token.Value<string>("PlayedAt") == "0000-00-00")
            {
                token["PlayedAt"] = null;
            }

            token["Copyright"] = url;
        }

        private static async Task SendRequest(HttpClient client, string url, QuestionDataSql questionData, Tournament parentTournament = null)
        {
            string xml = "";
            string fullUrl = $"https://db.chgk.info{url}/xml";
            Trace.WriteLine("");
            Trace.WriteLine(fullUrl);
            Trace.WriteLine("");
            try
            {
                xml = await client.GetStringAsync(fullUrl);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when get request", ex);
            }

            JObject token = ParseString(xml);

            try
            {
                FixSchemaErrors(fullUrl, ref token);

                string type = token.Value<string>("Type");
                int id = token.Value<int>("Id");

                if (type == "Г")
                {
                    var tours = token["tour"];

                    if (!(tours is JArray))
                    {
                        token["tour"] = new JArray { tours };
                    }

                    var childTournaments = (token["tour"] as JArray).Select(x => $"/tour/{x["TextId"]}");//Ч

                    token["tour"] = null;

                    Tournament tournament = token.ToObject<Tournament>();
                    questionData.InsertTournament(tournament);

                    foreach (string tournamentUrl in childTournaments)
                    {
                        await SendRequest(client, tournamentUrl, questionData);
                    }
                }
                else if (type == "Ч")
                {
                    var questions = token["question"];
                    var tours = new List<string>();


                    if (questions != null)
                    {
                        if (!(questions is JArray))
                        {
                            token["question"] = new JArray { questions };
                        }
                                               
                        foreach (var x in (token["question"] as JArray))
                        {
                            var t = x["ParentTextId"];
                            if (t.HasValues)
                            {
                                tours.Add($"/tour/{x["ParentTextId"]}");
                            }
                            else
                            {
                                string textId = "/tour/" + x["TextId"].ToString().Split('-')[0];
                                if (!tours.Contains(textId))
                                {
                                    tours.Add(textId);
                                }
                            }
                        }
                    }
                    else
                    {
                        var tour = token["tour"];

                        if (!(tour is JArray))
                        {
                            token["tour"] = new JArray { tour };
                        }

                        tours = (token["tour"] as JArray).Select(x => $"/tour/{x["TextId"]}")
                            .Distinct()
                            .ToList();//Ч

                        token["tour"] = null;
                    }


                    token["question"] = null;

                    Tournament tournament = token.ToObject<Tournament>();
                    questionData.InsertTournament(tournament);

                    foreach (string tourUrl in tours)
                    {
                        await SendRequest(client, tourUrl, questionData, tournament);
                    }
                }
                else if (type == "Т")
                {
                    JToken question = token["question"];
                    if (!(question is JArray))
                    {
                        token["question"] = new JArray { question };
                    }

                    Tour tour = token.ToObject<Tour>();
                    questionData.InsertTour(tour, parentTournament);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception when inserting to DB, {ex.Message}", ex);
            }
        }
    }
}
