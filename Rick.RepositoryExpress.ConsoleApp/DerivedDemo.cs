using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.ConsoleApp
{
    class DerivedDemo : BaseDemo
    {
        public DerivedDemo()
        {
            this.Name = "张三";
        }
        public string Name { get; set; }

    }
}
