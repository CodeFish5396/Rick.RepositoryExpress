using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Channelprice
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public long Nationid { get; set; }
        public decimal Minweight { get; set; }
        public decimal Maxweight { get; set; }
        public decimal Price { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
    }
}
