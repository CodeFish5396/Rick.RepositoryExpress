using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Userchargeqrcodeimage
    {
        public long Id { get; set; }
        public long Fileinfoid { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
    }
}
