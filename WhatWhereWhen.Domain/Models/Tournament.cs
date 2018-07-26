using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tournament : TourBase
    {
        public override TourType TourType => TourType.Tournament;

        [JsonProperty("question")]
        public IList<QuestionItem> Questions { get; set; }
    }
}
