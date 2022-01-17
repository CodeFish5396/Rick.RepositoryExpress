using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Agentfeeaccount
    {
        public long Id { get; set; }
        public long Agentid { get; set; }
        public long Currencyid { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
    }
}
