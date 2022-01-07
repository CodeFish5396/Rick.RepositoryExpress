using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Agentandcourier
    {
        public long Id { get; set; }
        public long Agentid { get; set; }
        public long Courierid { get; set; }
        public int Status { get; set; }
    }
}
