using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Domain.Interfaces
{
    public interface IQuestionData
    {
        int InsertOrUpdateQuestion(QuestionItem question);

        Task<QuestionItem> GetRandomQuestion(string conversationId, QuestionComplexity? complexity = QuestionComplexity.Middle, bool markAsRead = true);

        int InsertTournament(Tournament tournament);
        int InsertTour(Tour tour);

        Task<IEnumerable<Tour>> GetTours(byte level = 1);

        Task<TourBase> GetTourById(int tourId);
    }
}
