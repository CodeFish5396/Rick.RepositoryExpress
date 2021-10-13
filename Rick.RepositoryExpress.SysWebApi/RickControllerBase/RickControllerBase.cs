using Microsoft.AspNetCore.Mvc;
using Rick.RepositoryExpress.SysWebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi
{
    public class RickControllerBase : ControllerBase
    {
        public UserInfo UserInfo
        {
            get
            {
                return (UserInfo)base.HttpContext.Items["TokenUserInfo"];
            }
        }

    }
}
