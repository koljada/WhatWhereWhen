using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql.Mappings
{
    public class TourMapper : BaseMapper<Tour>
    {
        public TourMapper()
        {            
            Map(x => x.Questions).Ignore();

            AutoMap();
        }
    }
}
