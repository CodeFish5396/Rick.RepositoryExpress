using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Fileinfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Ext { get; set; }
        public string Mime { get; set; }
        public string Filename { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
    }
}
