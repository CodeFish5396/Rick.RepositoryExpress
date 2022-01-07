using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Currencychangerate
    {
        public long Id { get; set; }
        public long Sourcecurrency { get; set; }
        public long Targetcurrency { get; set; }
        public decimal Rate { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public DateTime Lasttime { get; set; }
        public long Lastuser { get; set; }
    }
}
