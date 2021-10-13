using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appuseraddress
    {
        public long Id { get; set; }
        public long Appuser { get; set; }
        public long Nationid { get; set; }
        public int Weight { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Name { get; set; }
        public string Contactnumber { get; set; }
        public string Region { get; set; }
        public string Address { get; set; }
    }
}
