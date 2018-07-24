using DapperExtensions.Mapper;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql.Mappings
{
    public class TournamentMapper : BaseMapper<Tournament>
    {
        public TournamentMapper()
        {
            Map(x => x.Questions).Ignore();

            AutoMap();
        }
    }
}
