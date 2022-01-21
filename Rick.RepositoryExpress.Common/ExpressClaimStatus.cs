using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum ExpressClaimStatus
    {
        无效 = 0,
        预报 = 1,
        已入库 = 2,
        已揽收 = 3,
        待验货 = 4,
        待退货 = 5,
        待换货 = 6,
        已验货 = 7,
        已退货 = 8,
        已换货 = 9,
        申请打包 = 10,
        发货待确认 = 11,
        待发货 = 12,
        已发货 = 13,
        已签收 = 14
    }
}
