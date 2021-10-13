using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Preexpressorderdetail
    {
        public long Id { get; set; }
        public long Preexpressorderid { get; set; }
        public string Packagename { get; set; }
        public decimal? Unitprice { get; set; }
        public string Count { get; set; }
    }
}
