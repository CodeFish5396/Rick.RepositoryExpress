using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.DataBase.ViewModels
{
    public class PackageInView
    {
        public long Userid { get; set; }
        public long Packageid { get; set; }
        public int Count { get; set; }
        public string Username { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Courierid { get; set; }
        public string Couriername { get; set; }
        public string Expressnumber { get; set; }
        public long Lastuser { get; set; }
        public string Lastusername { get; set; }
    }
}
