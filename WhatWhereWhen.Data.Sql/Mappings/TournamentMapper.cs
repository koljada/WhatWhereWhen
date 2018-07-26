using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql.Mappings
{
    public class TournamentMapper : BaseMapper<Tournament>
    {
        public TournamentMapper()
        {
            TableName = "Tour";

            Map(x => x.Questions).Ignore();

            AutoMap();
        }
    }
}
