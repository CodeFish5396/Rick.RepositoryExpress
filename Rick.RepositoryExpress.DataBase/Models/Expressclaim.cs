using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Expressclaim
    {
        public long Id { get; set; }
        public long Expressinfoid { get; set; }
        public long Repositoryid { get; set; }
        public long Appuser { get; set; }
        public string Remark { get; set; }
        public int Count { get; set; }
        public sbyte Cansendasap { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
