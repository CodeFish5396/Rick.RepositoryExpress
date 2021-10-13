using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageimage
    {
        public long Id { get; set; }
        public long Packageid { get; set; }
        public long Fileinfoid { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
    }
}
