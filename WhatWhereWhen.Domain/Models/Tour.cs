using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tour : TourBase
    {
        [JsonProperty("question")]
        public IList<QuestionItem> Questions { get; set; }

    }
}
