using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Runfee
    {
        public long Id { get; set; }
        public long Accountid { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public int Paytype { get; set; }
        public DateTime Paytime { get; set; }
    }
}
