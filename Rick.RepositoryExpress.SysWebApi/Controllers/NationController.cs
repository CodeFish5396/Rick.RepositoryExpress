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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 国家
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NationController : RickControllerBase
    {
        private readonly ILogger<NationController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly INationService _nationService;
        private readonly RedisClientService _redisClientService;

        public NationController(ILogger<NationController> logger, INationService nationService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _nationService = nationService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询国家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<NationResponse>>> Get()
        {
            var results = await _nationService.QueryAsync<Nation>(t => t.Status == 1);
            return RickWebResult.Success(results.Select(t => new NationResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name
            }));
        }

        /// <summary>
        /// 创建国家
        /// </summary>
        /// <param name="nationRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<NationResponse>> Post([FromBody] NationRequest nationRequest)
        {
            await _nationService.BeginTransactionAsync();

            Nation nation = new Nation();
            DateTime now = DateTime.Now;
            nation.Id = _idGenerator.NextId();
            nation.Name = nationRequest.Name;
            nation.Code = nationRequest.Code;
            nation.Status = 1;
            nation.Addtime = now;
            nation.Lasttime = now;
            nation.Adduser = UserInfo.Id;
            nation.Lastuser = UserInfo.Id;
            await _nationService.AddAsync(nation);
            await _nationService.CommitAsync();
            NationResponse nationResponse = new NationResponse();
            nationResponse.Id = nation.Id;
            nationResponse.Name = nation.Name;
            nationResponse.Code = nation.Code;
            return RickWebResult.Success(nationResponse);

        }

    }

    public class NationRequest
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class NationResponse
    {
        public long Id { get; set; }
        public string Code { get; set; }

        public string Name { get; set; }
    }

}
