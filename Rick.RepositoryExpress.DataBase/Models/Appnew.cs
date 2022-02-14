using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appnew
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public int Type { get; set; }
        public string Vicetitle { get; set; }
        public long Urlid { get; set; }
        public long Imageid { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public DateTime Lasttime { get; set; }
        public long Lastuser { get; set; }
        public int Isshow { get; set; }
        public int Source { get; set; }
    }
}
