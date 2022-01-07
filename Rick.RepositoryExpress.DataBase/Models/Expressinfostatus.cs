using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Expressinfostatus
    {
        public long Id { get; set; }
        public long Expressinfoid { get; set; }
        public DateTime Addtime { get; set; }
        public string Location { get; set; }
        public DateTime Searchtime { get; set; }
    }
}
