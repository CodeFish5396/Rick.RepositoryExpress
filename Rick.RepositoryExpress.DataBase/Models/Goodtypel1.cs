﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Rick.RepositoryExpress.DataBase.Models
{
    public partial class Goodtypel1
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public int Order { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Adduser { get; set; }
        public long Lastuser { get; set; }
    }
}
