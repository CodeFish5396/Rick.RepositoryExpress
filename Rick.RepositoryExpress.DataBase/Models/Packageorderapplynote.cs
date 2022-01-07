using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplynote
    {
        public long Id { get; set; }
        public long Packageorderapplyid { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public int Status { get; set; }
        public int Isclosed { get; set; }
        public int Operator { get; set; }
        public long? Operatoruser { get; set; }
    }
}
