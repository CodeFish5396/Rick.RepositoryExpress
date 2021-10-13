using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packagerefundapply
    {
        public long Id { get; set; }
        public long Packageid { get; set; }
        public long Exclaimid { get; set; }
        public long Appuser { get; set; }
        public int Refundstatus { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public DateTime Lastuser { get; set; }
        public long Lasttime { get; set; }
        public string Code { get; set; }
        public string Remark { get; set; }
    }
}
