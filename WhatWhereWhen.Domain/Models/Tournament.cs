using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tournament : TourBase
    {        
        [JsonProperty("tour")]
        public IList<TourBase> Tours { get; set; }
    }
}
