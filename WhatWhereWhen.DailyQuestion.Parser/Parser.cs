using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using WhatWhereWhen.Data.Sql;
using WhatWhereWhen.Domain.Interfaces;

namespace WhatWhereWhen.DailyQuestion.Parser
{
    public static class Parser
    {
        [FunctionName("ParseQuestion")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start parsing function executed at: {DateTime.Now}");

            int limit = 100;
            int.TryParse(GetEnvironmentVariable("Limit"), out limit);

            string connStr = GetEnvironmentVariable("DefaultConnection");

            IQuestionData questionData = new QuestionDataSql(connStr);

            using (HttpClient client = new HttpClient())
            {
                await SendRequest(client, questionData, limit, log);
            }

            log.Info($"End parsing function executed at: {DateTime.Now}");
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static async Task SendRequest(HttpClient client, IQuestionData questionData, int limit, TraceWriter log)
        {
            string url = GetEnvironmentVariable("Url");
            url += limit;

            log.Info("Sending a request to " + url);
            string xml = "";
            string json = null;
            QuestionCollection data = null;

            try
            {
                xml = await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                log.Error("Exception has been thrown when sending a put request", ex);
                return;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                log.Error("Exception has been thrown when parsing xml into XDocument", ex);
                return;
            }

            try
            {
                json = JsonConvert.SerializeXmlNode(doc.LastChild, Newtonsoft.Json.Formatting.None, true);
            }
            catch (Exception ex)
            {
                log.Error("Exception has been thrown when parsing xml into json string", ex);
                return;
            }

            try
            {
                data = JsonConvert.DeserializeObject<QuestionCollection>(json);
            }
            catch (Exception ex)
            {
                log.Error("Exception has been thrown when deserializing json string", ex);
                return;
            }

            Parallel.ForEach(data.Questions, question =>
            {
                try
                {
                    questionData.InsertOrUpdateQuestion(question);
                }
                catch (Exception ex)
                {
                    log.Error("Exception has been thrown when inserting a question", ex);
                }
            });
        }
    }
}
