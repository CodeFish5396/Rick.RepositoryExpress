using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Expressinfo
    {
        public long Id { get; set; }
        public string Expressnumber { get; set; }
        public long? Courierid { get; set; }
        public int? Expressstatus { get; set; }
        public string Sendername { get; set; }
        public string Sendermobil { get; set; }
        public string Senderaddress { get; set; }
        public string Recivername { get; set; }
        public string Recivermobil { get; set; }
        public string Reciversddress { get; set; }
        public string Currentdetails { get; set; }
        public string Lastdetails { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
