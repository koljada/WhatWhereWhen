using Newtonsoft.Json;
using System.Collections.Generic;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.DailyQuestion.Parser
{
    public class QuestionCollection
    {
        [JsonProperty("question")]
        public IList<QuestionItem> Questions { get; set; }
    }
}
