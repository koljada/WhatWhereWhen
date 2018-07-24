using System.Threading.Tasks;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Domain.Interfaces
{
    public interface IQuestionData
    {
        int InsertOrUpdateQuestion(QuestionItem question);

        Task<QuestionItem> GetRandomQuestion(string conversationId, QuestionComplexity? complexity = QuestionComplexity.Middle, bool markAsRead = true);

        int InsertTournament(Tournament tournament);
    }
}
