using System;

namespace WhatWhereWhen.Domain.Models
{
    public class BaseEntity
    {        
        public virtual int Id { get; set; }

        public DateTime? ImportedAt { get; set; }
    }
}
