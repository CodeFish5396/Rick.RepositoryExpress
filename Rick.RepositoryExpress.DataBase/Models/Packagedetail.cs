using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packagedetail
    {
        public long Id { get; set; }
        public long Packageid { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Count { get; set; }
        public string Code { get; set; }
        public string Location { get; set; }
        public sbyte Hasprinttags { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
