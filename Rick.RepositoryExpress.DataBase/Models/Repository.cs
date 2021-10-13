using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Repository
    {
        public long Id { get; set; }
        public long Companyid { get; set; }
        public string Name { get; set; }
        public string Recivername { get; set; }
        public string Recivermobil { get; set; }
        public string Region { get; set; }
        public string Address { get; set; }
        public int Satus { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
