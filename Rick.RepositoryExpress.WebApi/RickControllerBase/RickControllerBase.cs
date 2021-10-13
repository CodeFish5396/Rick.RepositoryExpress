using Microsoft.AspNetCore.Mvc;
using Rick.RepositoryExpress.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.WebApi
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
