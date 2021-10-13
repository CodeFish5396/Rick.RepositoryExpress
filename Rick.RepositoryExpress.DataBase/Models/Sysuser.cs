using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Sysuser
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Truename { get; set; }
        public string Mobile { get; set; }
        public int Sex { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
    }
}
