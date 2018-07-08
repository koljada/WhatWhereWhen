using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace WhatWhereWhen.DailyQuestionJob
{
    public class Functions
    {
        public static async Task PostNewQuestion([TimerTrigger("00:10:00")] TimerInfo timer, TraceWriter log)
        {
            log.Verbose($"Web Job started execution: {DateTime.Now}");

            try
            {
                string url = ConfigurationManager.AppSettings["NewQuestionUrl"];

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.PutAsync(url, new StringContent(""));
                    log.Info("Response received: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception has been thorwn: " + ex.ToString());
            }

            log.Verbose($"Web Job finished execution: {DateTime.Now}");
        }
    }
}
