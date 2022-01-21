using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appuserdatum
    {
        public long Id { get; set; }
        public long Type { get; set; }
        public long? Curencyid { get; set; }
        public decimal Amount { get; set; }
        public long Appuser { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Paytype { get; set; }
        public long? Orderid { get; set; }
    }
}
