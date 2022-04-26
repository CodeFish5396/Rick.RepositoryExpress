using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SysUserExtraController : RickControllerBase
    {
        private readonly ISysuserService _sysuserService;

        public SysUserExtraController(ILogger<SysUserExtraController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
        
            _sysuserService = sysuserService;
        }



        /// <summary>
        /// 修改系统用户信息
        /// </summary>
        /// <param name="sysUserPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] SysUserPutRequest sysUserPutRequest)
        {
            await _sysuserService.BeginTransactionAsync();

            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Id == sysUserPutRequest.Id);
            DateTime now = DateTime.Now;
           
            if(sysUserPutRequest.Disable.HasValue) sysuser.Status = sysUserPutRequest.Disable.Value?2:1;
            
            sysuser.Lasttime = now;
            sysuser.Lastuser = UserInfo.Id;
            await _sysuserService.UpdateAsync(sysuser);

            await _sysuserService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        
  

        public class SysUserPutRequest
        {
            public long Id { get; set; }
            public bool? Disable { get; set; }

        }
 


    }
}
