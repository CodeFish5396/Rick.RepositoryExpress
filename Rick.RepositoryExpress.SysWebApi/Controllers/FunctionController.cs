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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FunctionController : RickControllerBase
    {
        private readonly ILogger<FunctionController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFunctionService _functionService;
        private readonly RedisClientService _redisClientService;

        public FunctionController(ILogger<FunctionController> logger, IFunctionService functionService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _functionService = functionService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 提交Function
        /// </summary>
        /// <param name="functionPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [Admin]
        public async Task<RickWebResult<object>> Post([FromBody] FunctionPostRequest functionPostRequest)
        {
            await _functionService.BeginTransactionAsync();

            Sysfunction sysfunction = new Sysfunction();
            DateTime now = DateTime.Now;
            sysfunction.Id = _idGenerator.NextId();
            sysfunction.Name = functionPostRequest.Name;
            sysfunction.Typename = functionPostRequest.Typename;
            sysfunction.Status = 1;
            sysfunction.Addtime = now;
            sysfunction.Lasttime = now;
            sysfunction.Adduser = UserInfo.Id;
            sysfunction.Lastuser = UserInfo.Id;
            await _functionService.AddAsync(sysfunction);
            await _functionService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class FunctionPostRequest
        {
            public string Name { get; set; }
            public string Typename { get; set; }

        }

    }
}
