using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Syssetting
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
    }
}
