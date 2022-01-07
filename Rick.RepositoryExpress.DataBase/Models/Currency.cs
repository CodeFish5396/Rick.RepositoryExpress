using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Currency
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public DateTime Lasttime { get; set; }
        public long Lastuser { get; set; }
        public int Islocal { get; set; }
        public int Isdefault { get; set; }
    }
}
