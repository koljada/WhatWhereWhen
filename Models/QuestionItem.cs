using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Microsoft.Bot.Sample.SimpleEchoBot.Models
{
    public class QuestionItem : TableEntity
    {
        public string TourFileName { get; set; }
        public string TournamentFileName { get; set; }
        public long QuestionId { get; set; }
        public long ParentId { get; set; }
        public int Number { get; set; }
        public string Type { get; set; }
        public int TypeNum { get; set; }
        public string TextId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string PassCriteria { get; set; }
        public string Authors { get; set; }
        public string Sources { get; set; }
        public string Comments { get; set; }
        public string Rating { get; set; }
        public double RatingNumber { get; set; }
        public int Complexity { get; set; }
        public string Topic { get; set; }
        public string ProcessedBySearch { get; set; }
        public string Parent_text_id { get; set; }
        public string ParentTextId { get; set; }
        public int TourId { get; set; }
        public int TournamentId { get; set; }
        public string TourTitle { get; set; }
        public string TournamentTitle { get; set; }
        public string TourType { get; set; }
        public string TournamentType { get; set; }
        public DateTime? TourPlayedAt { get; set; }
        public DateTime? TournamentPlayedAt { get; set; }
        public string Notices { get; set; }

        public string QuestonPictureUrl { get; set; }
        public string AnswerPictureUrl { get; set; }

        public QuestionItem Copy()
        {
            return new QuestionItem
            {
                TourFileName = TourFileName,
                TournamentFileName = TournamentFileName,
                QuestionId = QuestionId,
                ParentId = ParentId,
                Number = Number,
                Type = Type,
                TypeNum = TypeNum,
                TextId = TextId,
                Question = Question,
                Answer = Answer,
                PassCriteria = PassCriteria,
                Authors = Authors,
                Sources = Sources,
                Comments = Comments,
                Rating = Rating,
                RatingNumber = RatingNumber,
                Complexity = Complexity,
                Topic = Topic,
                ProcessedBySearch = ProcessedBySearch,
                Parent_text_id = Parent_text_id,
                ParentTextId = ParentTextId,
                TourId = TourId,
                TournamentId = TournamentId,
                TourTitle = TourTitle,
                TournamentTitle = TournamentTitle,
                TourType = TourType,
                TournamentType = TournamentType,
                TourPlayedAt = TourPlayedAt,
                TournamentPlayedAt = TournamentPlayedAt,
                Notices = Notices,
                QuestonPictureUrl = QuestonPictureUrl,
                AnswerPictureUrl = AnswerPictureUrl,

                Timestamp = DateTime.UtcNow,
                PartitionKey = true.ToString(),
                RowKey = RowKey
            };
        }
    }
}