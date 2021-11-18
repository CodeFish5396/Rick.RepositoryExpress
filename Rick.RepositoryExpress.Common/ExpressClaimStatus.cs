using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum ExpressClaimStatus
    {
        无效 = 0,
        正常 = 1,
        已到库 = 2,
        已入库 = 3,
        已申请 = 4,
        已发货 = 5
    }
}
