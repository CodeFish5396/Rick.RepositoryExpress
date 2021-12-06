using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplyexpressdetail
    {
        public long Id { get; set; }
        public long Packageorderapplyexpressid { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volumeweight { get; set; }
        public string Innernumber { get; set; }
        public string Batchnumber { get; set; }
        public int? Forseerecieveday { get; set; }
        public string Outnumber { get; set; }
        public int? Count { get; set; }
        public decimal? Customprice { get; set; }
        public decimal? Sueprice { get; set; }
        public decimal? Overlengthprice { get; set; }
        public decimal? Overweightprice { get; set; }
        public decimal? Oversizeprice { get; set; }
        public decimal? Paperprice { get; set; }
        public decimal? Boxprice { get; set; }
        public decimal? Bounceprice { get; set; }
        public decimal? Price { get; set; }
        public decimal? Agentprice { get; set; }
        public decimal? Agentcurrency { get; set; }
    }
}
