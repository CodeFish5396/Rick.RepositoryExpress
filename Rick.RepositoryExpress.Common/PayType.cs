using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum PayType
    {
        无效 = 0,
        银行转帐 = 1,
        微信转帐 = 2,
        支付宝转帐 = 3,
        现金 = 4,
        活动赠送 = 5
    }
}
