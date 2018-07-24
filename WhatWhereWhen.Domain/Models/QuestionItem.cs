using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WhatWhereWhen.Domain.Models
{
    [Serializable]
    public class QuestionItem : BaseEntity
    {
        public QuestionItem()
        {
            QuestionImageUrls = new List<string>();
            AnswerImageUrls = new List<string>();
        }

        [JsonProperty("QuestionId")]
        public override int Id { get; set; }

        [JsonProperty("ParentId")]
        public int ParentId { get; set; }

        [JsonProperty("Number")]
        public short Number { get; set; }

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
        public double? RatingNumber { get; set; }

        [JsonProperty("Complexity")]
        public byte? Complexity { get; set; }

        [JsonProperty("tourId")]
        public int? TourId { get; set; }//FK to Tour

        [JsonProperty("tournamentId")]
        public int? TournamentId { get; set; }//FK to Tournament

        [JsonProperty("tourTitle")]
        public string TourTitle { get; set; }//TODO: From Tour

        [JsonProperty("tournamentTitle")]
        public string TournamentTitle { get; set; }//TODO: From Tournament

        [JsonProperty("tourType")]
        public string TourType { get; set; }//TODO: From Tour

        [JsonProperty("tournamentType")]
        public string TournamentType { get; set; }

        [JsonProperty("tourPlayedAt")]
        public DateTime? TourPlayedAt { get; set; }//TODO: From Tour

        [JsonProperty("tournamentPlayedAt")]
        public DateTime? TournamentPlayedAt { get; set; }//TODO: From Tournament

        [JsonProperty("Notices")]
        public string Notices { get; set; }

        [JsonProperty("Topic")]
        public string Topic { get; set; }

        public DateTime? ImportedAt { get; set; }

        public IEnumerable<string> QuestionImageUrls { get; set; }
        public IEnumerable<string> AnswerImageUrls { get; set; }

        public void InitUrls(string baseUrl)
        {
            Question = GetImageUrl(Question, baseUrl, out IList<string> questionImageUrls);
            Answer = GetImageUrl(Answer, baseUrl, out IList<string> answerImageUrls);

            QuestionImageUrls = questionImageUrls;
            AnswerImageUrls = answerImageUrls;
        }

        private string GetImageUrl(string text, string baseUrl, out IList<string> urls)
        {
            urls = new List<string>();
            string regexpPattern = "\\(pic: (.*)\\)";

            var mathces = Regex.Matches(text, regexpPattern);

            foreach (Match match in mathces)
            {
                string url = match.Groups[1].Value;
                urls.Add(baseUrl + url);
            }

            return Regex.Replace(text, regexpPattern, "");
        }
    }
}
