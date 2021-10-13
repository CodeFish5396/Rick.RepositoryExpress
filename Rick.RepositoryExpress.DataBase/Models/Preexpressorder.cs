using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Preexpressorder
    {
        public long Id { get; set; }
        public string Expressnumber { get; set; }
        public long Prerepositoryid { get; set; }
        public long Precourierid { get; set; }
        public long Appuserid { get; set; }
        public string Remark { get; set; }
        public int Status { get; set; }
        public sbyte Cansendasap { get; set; }
    }
}
