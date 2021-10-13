using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Appuseraccountdetail
    {
        public long Id { get; set; }
        public long Useraccountid { get; set; }
        public int Direction { get; set; }
        public string Remark { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
    }
}
