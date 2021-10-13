using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appuser
    {
        public long Id { get; set; }
        public string Openid { get; set; }
        public string Mobile { get; set; }
        public string Countrycode { get; set; }
        public string Name { get; set; }
        public string Headportrait { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public int Status { get; set; }
        public long? AddUser { get; set; }
    }
}
