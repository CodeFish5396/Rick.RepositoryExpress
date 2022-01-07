using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Messageconsume
    {
        public long Id { get; set; }
        public long Messageid { get; set; }
        public long Sysuser { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
    }
}
