using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum OrderApplyStatus
    {
        无效 = 0,
        发起申请 = 1,
        出货录单 = 2,
        确认发货 = 3,
        已发货 = 4
    }
}
