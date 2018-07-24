using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class Tournament : BaseEntity
    {
        [JsonProperty("Id")]
        public override int Id { get; set; }

        [JsonProperty("ParentId")]
        public int TourId { get; set; }//FK to tournament

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("Number")]
        public short? Number { get; set; }

        //[JsonProperty("TextId")]
        //public string TextId { get; set; }

        [JsonProperty("QuestionsNum")]
        public short QuestionsNum { get; set; }

        [JsonProperty("Complexity")]
        public byte? Complexity { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Copyright")]
        public string Copyright { get; set; }

        [JsonProperty("Info")]
        public string Info { get; set; }

        [JsonProperty("URL")]
        public string URL { get; set; }

        //[JsonProperty("FileName")]
        //public string FileName { get; set; }

        //[JsonProperty("RatingId")]
        //public string RatingId { get; set; }

        [JsonProperty("Editors")]
        public string Editors { get; set; }

        //[JsonProperty("EnteredBy")]
        //public string EnteredBy { get; set; }

        [JsonProperty("LastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [JsonProperty("PlayedAt")]
        public DateTime? PlayedAt { get; set; }

        //[JsonProperty("KandId")]
        //public string KandId { get; set; }

        [JsonProperty("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        public DateTime ImportedAt { get; set; }

        //[JsonProperty("ParentTextId")]
        //public string ParentTextId { get; set; }

        //[JsonProperty("ChildrenNum")]
        //public short ChildrenNum { get; set; }

        [JsonProperty("question")]
        public IList<QuestionItem> Questions { get; set; }
    }
}
