using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Channeldetail
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public long Nationid { get; set; }
        public long Agentid { get; set; }
        public decimal Unitprice { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
