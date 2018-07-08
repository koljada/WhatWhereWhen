using Microsoft.Azure.WebJobs;

namespace WhatWhereWhen.DailyQuestionJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            JobHost host = new JobHost(config);

            config.UseTimers();

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
