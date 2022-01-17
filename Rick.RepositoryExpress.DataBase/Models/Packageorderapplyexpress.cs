using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Packageorderapplyexpress
    {
        public long Id { get; set; }
        public long Packageorderapplyid { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public string Remark { get; set; }
        public string Innernumber { get; set; }
        public string Batchnumber { get; set; }
        public string Mailcode { get; set; }
        public long? Currencychangerateid { get; set; }
        public decimal? Currencychangerate { get; set; }
        public decimal? Price { get; set; }
        public decimal? Targetprice { get; set; }
        public long? Agentid { get; set; }
        public long? Courierid { get; set; }
        public string Couriercode { get; set; }
        public string Outnumber { get; set; }
        public decimal? Freightprice { get; set; }
        public decimal? Agentprice { get; set; }
        public decimal? Localagentprice { get; set; }
        public long? Agentcurrencychangerateid { get; set; }
        public decimal? Agentcurrencychangerate { get; set; }
        public int? Totalcount { get; set; }
        public decimal? Totalweight { get; set; }
    }
}
