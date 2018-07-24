using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = new HttpClient())
            {
                SendRequest(client, "https://db.chgk.info/tour/asker93/xml").Wait();
            }
        }

        private static async Task SendRequest(HttpClient client, string url)
        {           
            string xml = "";
            string json = null;

            try
            {
                xml = await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            try
            {
                json = JsonConvert.SerializeXmlNode(doc.LastChild, Newtonsoft.Json.Formatting.None, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<Tournament>(json);
                var repo = new QuestionDataSql();
                repo.InsertTournament(data);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            //Parallel.ForEach(data.Questions, question =>
            //{
            //    try
            //    {
            //        questionData.InsertOrUpdateQuestion(question);
            //    }
            //    catch (Exception ex)
            //    {
            //        log.Error("Exception has been thrown when inserting a question", ex);
            //    }
            //});
        }
    }
}
