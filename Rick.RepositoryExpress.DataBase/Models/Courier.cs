using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Courier
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Extname { get; set; }
        public int Status { get; set; }
        public string Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public string Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
