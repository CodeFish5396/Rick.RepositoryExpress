using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Channelworkday
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public string Workday { get; set; }
    }
}
