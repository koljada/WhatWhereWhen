using DapperExtensions.Mapper;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql.Mappings
{
    public class BaseMapper<T> : ClassMapper<T> where T : BaseEntity
    {
        public BaseMapper()
        {
            SchemaName = "cgk";

            Map(x => x.Id).Key(KeyType.Assigned);

            
        }
    }
}
