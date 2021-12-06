using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UseraccountconsumeController : RickControllerBase
    {
        private readonly ILogger<UseraccountconsumeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraccountService _appuseraccountService;
        private readonly RedisClientService _redisClientService;

        public UseraccountconsumeController(ILogger<UseraccountconsumeController> logger, IAppuseraccountService appuseraccountService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraccountService = appuseraccountService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户消费
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppuseraccountconsumeResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            AppuseraccountconsumeResponseList appuseraccountconsumeResponseList = new AppuseraccountconsumeResponseList();

            var query = from consume in _appuseraccountService.Query<Appuseraccountconsume>(t => t.Status == 1 && t.Appuser == UserInfo.Id)
                        select new AppuseraccountconsumeResponse() {
                            Id = consume.Id,
                            Amount = consume.Amount,
                            Addtime = consume.Addtime,
                        };
            appuseraccountconsumeResponseList.Count = await query.CountAsync();
            appuseraccountconsumeResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(appuseraccountconsumeResponseList);
        }
        public class AppuseraccountconsumeResponse
        {
            public long Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime Addtime { get; set; }
        }
        public class AppuseraccountconsumeResponseList
        { 
            public int Count { get; set; }
            public List<AppuseraccountconsumeResponse> List { get; set; }
        }

    }
}
