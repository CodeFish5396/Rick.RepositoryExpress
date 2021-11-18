using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapply
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public long Nationid { get; set; }
        public long Addressid { get; set; }
        public long Appuser { get; set; }
        public int Orderstatus { get; set; }
        public sbyte? Ispayed { get; set; }
        public DateTime? Paytime { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Remark { get; set; }
    }
}
