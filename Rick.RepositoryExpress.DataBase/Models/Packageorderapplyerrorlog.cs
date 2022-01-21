using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplyerrorlog
    {
        public long Id { get; set; }
        public int Type { get; set; }
        public long Packageorderapplyerrorid { get; set; }
        public long Appuser { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Remark { get; set; }
    }
}
