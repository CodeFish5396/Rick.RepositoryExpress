using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public enum PackageNoteStatus
    {
        无效 = 0,
        入库录单 = 1,
        包裹入库 = 2,
        申请发货 = 3,
        申请退货 = 4,
        申请换货 = 5,
        已发货 = 6,
        已退货 = 7,
        已换货 = 8
    }
}
