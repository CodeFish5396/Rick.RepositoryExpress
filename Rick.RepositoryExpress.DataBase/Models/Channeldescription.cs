using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Channeldescription
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
    }
}
