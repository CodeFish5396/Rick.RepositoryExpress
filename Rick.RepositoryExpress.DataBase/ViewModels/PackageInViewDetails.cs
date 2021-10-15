using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.DataBase.ViewModels
{
    public class PackageInViewDetails
    {
        public long Packageid { get; set; }
        public IList<long> Images { get; set; }
        public IList<long> Videos { get; set; }
    }
}
