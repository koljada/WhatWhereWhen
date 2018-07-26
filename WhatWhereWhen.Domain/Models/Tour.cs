using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tour : TourBase
    {
        public override TourType TourType => TourType.Tour;

        [JsonProperty("tour")]
        public IList<Tour> Tours { get; set; }
    }
}
