using Newtonsoft.Json;
using System;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class QuestionItem
    {
        [JsonProperty("QuestionId")]
        public int Id { get; set; }

        [JsonProperty("ParentId")]
        public int ParentId { get; set; }

        [JsonProperty("Number")]
        public byte Number { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("TypeNum")]
        public string TypeNum { get; set; }

        [JsonProperty("Question")]
        public string Question { get; set; }

        [JsonProperty("Answer")]
        public string Answer { get; set; }

        [JsonProperty("PassCriteria")]
        public string PassCriteria { get; set; }

        [JsonProperty("Authors")]
        public string Authors { get; set; }

        [JsonProperty("Sources")]
        public string Sources { get; set; }

        [JsonProperty("Comments")]
        public string Comments { get; set; }

        [JsonProperty("Rating")]
        public string Rating { get; set; }

        [JsonProperty("RatingNumber")]
        public double RatingNumber { get; set; }

        [JsonProperty("Complexity")]
        public byte Complexity { get; set; }

        [JsonProperty("tourId")]
        public int? TourId { get; set; }

        [JsonProperty("tournamentId")]
        public int? TournamentId { get; set; }

        [JsonProperty("tourTitle")]
        public string TourTitle { get; set; }

        [JsonProperty("tournamentTitle")]
        public string TournamentTitle { get; set; }

        [JsonProperty("tourType")]
        public string TourType { get; set; }

        [JsonProperty("tournamentType")]
        public string TournamentType { get; set; }

        [JsonProperty("tourPlayedAt")]
        public DateTime? TourPlayedAt { get; set; }

        [JsonProperty("tournamentPlayedAt")]
        public DateTime? TournamentPlayedAt { get; set; }

        [JsonProperty("Notices")]
        public string Notices { get; set; }

        [JsonProperty("Topic")]
        public string Topic { get; set; }
    }
}
