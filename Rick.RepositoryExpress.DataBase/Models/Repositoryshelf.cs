using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Repositoryshelf
    {
        public long Id { get; set; }
        public long Repositoryid { get; set; }
        public long Repositoryregionid { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
        public int Status { get; set; }
    }
}
