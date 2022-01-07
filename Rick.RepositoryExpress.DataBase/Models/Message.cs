using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Message
    {
        public long Id { get; set; }
        public string Index { get; set; }
        public long Sender { get; set; }
        public string Message1 { get; set; }
        public int Isclosed { get; set; }
        public DateTime Addtime { get; set; }
        public long Adduser { get; set; }
        public DateTime Lasttime { get; set; }
        public long Lastuser { get; set; }
        public int Status { get; set; }
    }
}
