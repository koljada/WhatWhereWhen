using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tournament : TourBase
    {        
        [JsonProperty("tour")]
        public IList<Tour> Tours { get; set; }
    }
}
