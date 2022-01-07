using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Agentfee
    {
        public long Id { get; set; }
        public long Agentid { get; set; }
        public long Accountid { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public int Paytype { get; set; }
    }
}
