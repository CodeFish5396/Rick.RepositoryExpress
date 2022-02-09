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
using System.IO;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomePageController : RickControllerBase
    {
        private readonly ILogger<HomePageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public HomePageController(ILogger<HomePageController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取首页概览统计
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<object>> Get()
        {
            var env = Environment.GetEnvironmentVariables();
            var os = Convert.ToString(env["OS"]);
            var dr = Convert.ToString(env["SystemDrive"]);
            return RickWebResult.Success<object>(string.Format("{0}-{1}-{2}", os, dr, dr + "\\Uploads\\Channelpricedemo\\")) ;
            //_sysuserService.cre
        }
        
    }
}
