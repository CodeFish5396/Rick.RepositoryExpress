using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.ConsoleApp
{
    class BaseDemo
    {
        public BaseDemo()
        {
            this.Name = "张无忌";
        }
        public static int CurrentIndex { get; set; }
        public string Name { get; set; }
    }
}
