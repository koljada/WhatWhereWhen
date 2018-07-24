using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql.Mappings
{
    public class QuestionMapper : BaseMapper<QuestionItem>
    {
        public QuestionMapper()
        {
            TableName = "Question";

            Map(x => x.AnswerImageUrls).Ignore();
            Map(x => x.QuestionImageUrls).Ignore();

            AutoMap();
        }
    }
}
