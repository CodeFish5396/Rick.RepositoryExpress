using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum OrderApplyStatus
    {
        无效 = 0,
        申请打包 = 1,
        发货待确认 = 2,
        待发货 = 3,
        已发货 = 4,
        已签收 = 5
    }
}
