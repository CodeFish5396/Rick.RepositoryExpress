using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplyexpresspackage
    {
        public long Id { get; set; }
        public long Packageorderapplyexpressdetailsid { get; set; }
        public long Packageid { get; set; }
    }
}
