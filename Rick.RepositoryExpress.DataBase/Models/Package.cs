using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Package
    {
        public long Id { get; set; }
        public long Repositoryid { get; set; }
        public long Expressinfoid { get; set; }
        public long Courierid { get; set; }
        public string Expressnumber { get; set; }
        public string Code { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string Remark { get; set; }
        public int Status { get; set; }
        public long Adduser { get; set; }
        public DateTime Addtime { get; set; }
        public long Lastuser { get; set; }
        public DateTime Lasttime { get; set; }
        public long? Repositoryinuser { get; set; }
        public DateTime? Repositoryintime { get; set; }
        public string Changecode { get; set; }
        public string Refundcode { get; set; }
        public decimal? Freightprice { get; set; }
        public decimal? Localfreightprice { get; set; }
        public decimal? Currencychangerate { get; set; }
        public long? Currencychangerateid { get; set; }
        public string Checkremark { get; set; }
        public string Refundremark { get; set; }
        public string Changeremark { get; set; }
        public long? Repositoryregionid { get; set; }
        public long? Repositoryshelfid { get; set; }
        public long? Repositorylayerid { get; set; }
        public string Repositorynumber { get; set; }
        public int Claimtype { get; set; }
    }
}
