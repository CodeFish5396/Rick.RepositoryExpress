using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Courier
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Extname { get; set; }
        public sbyte Hasoutdoor { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public int Type { get; set; }
        public int Order { get; set; }
    }
}
