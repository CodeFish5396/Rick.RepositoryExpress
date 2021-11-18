using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplyexpress
    {
        public long Id { get; set; }
        public long Packageorderapplyid { get; set; }
        public long Channelid { get; set; }
        public long Countryid { get; set; }
        public long Addressid { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Remark { get; set; }
        public string Innernumber { get; set; }
        public string Batchnumber { get; set; }
        public int? Forseerecieveday { get; set; }
        public string Outnumber { get; set; }
        public int? Count { get; set; }
        public decimal? Weight { get; set; }
        public string Mailcode { get; set; }
        public string Customprice { get; set; }
        public decimal? Sueprice { get; set; }
        public decimal? Overlengthprice { get; set; }
        public decimal? Overweightprice { get; set; }
        public decimal? Oversizeprice { get; set; }
        public decimal? Paperprice { get; set; }
        public decimal? Boxprice { get; set; }
        public decimal? Bounceprice { get; set; }
        public decimal? Price { get; set; }
        public decimal? Agentprice { get; set; }
        public long? Agentcurrency { get; set; }
    }
}
