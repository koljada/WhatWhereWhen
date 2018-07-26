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
        static void Main(string[] args)
        {
            string[] links = GetUrls();

            IQuestionData questionData = new QuestionDataSql(true);

            using (HttpClient client = new HttpClient())
            {
                for (int i = 0; i < links.Length; i++)
                {
                    string link = links[i];
                    Trace.WriteLine("");
                    Trace.TraceInformation($"{i+1}. Start processing {link}");
                    Trace.WriteLine("");
                    SendRequest(client, link, questionData).Wait();
                    Trace.WriteLine("");
                    Trace.TraceInformation($"{i + 1}. End processing {link}");
                    Trace.WriteLine("");
                    Trace.WriteLine("******************************");
                    Trace.WriteLine("");
                }
            }

            Console.ReadLine();
        }

        private static string[] GetUrls()
        {
            return System.IO.File.ReadAllLines(@"D:\urls.txt");
        }


        private static async Task SendRequest(HttpClient client, string url, IQuestionData questionData)
        {
            string xml = "";
            string json = null;
            JToken token = null;

            try
            {
                xml = await client.GetStringAsync($"https://db.chgk.info{url}/xml");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when get request", ex);
                return;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when loading text to XmlDocument", ex);
                return;
            }

            try
            {
                json = JsonConvert.SerializeXmlNode(doc.LastChild, Newtonsoft.Json.Formatting.None, true);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when Serializing to XmlNode", ex);
                return;
            }

            try
            {
                token = JToken.Parse(json);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when parsing JToken", ex);
                return;
            }

            try
            {
                short chidrens = token.Value<short>("ChildrenNum");
                if (chidrens == 1)
                {
                    var tournament = token.ToObject<Tournament>();
                    questionData.InsertTournament(tournament);
                }
                else
                {
                    var tour = token.ToObject<Tour>();
                    questionData.InsertTour(tour);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception when inserting to DB", ex);
                return;
            }
        }
    }
}
