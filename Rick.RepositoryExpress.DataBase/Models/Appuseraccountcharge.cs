using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appuseraccountcharge
    {
        public long Id { get; set; }
        public long Appuser { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public string Remark { get; set; }
    }
}
