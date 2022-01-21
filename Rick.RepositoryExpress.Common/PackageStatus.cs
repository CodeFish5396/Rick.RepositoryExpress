using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum PackageStatus
    {
        无效 = 0,
        已入库 = 1,
        已入柜 = 2,
        待验货 = 3,
        待退货 = 4,
        待换货 = 5,
        已出库 = 6,
        已验货 = 7,
        已退货 = 8,
        已换货 = 9,
        问题件 = 10,

    }
}
