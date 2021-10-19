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

namespace Rick.RepositoryExpress.WebApi.Controllers
{
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
        /// 查询所有国家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<NationResponse>>> Get()
        {
            var result = await _nationService.QueryAsync<Nation>(nation => nation.Status == 1);
            return RickWebResult.Success(result.OrderBy(t => t.Order).Select(nation => new NationResponse()
            {
                Id = nation.Id,
                Code = nation.Code,
                Name = nation.Name,
                Order = nation.Order
            }));

        }

        public class NationRequest
        { 
        
        }
        public class NationResponse
        {
            public long Id { get; set; }

            public string Code { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }

        }
        public class NationResponseList
        {

        }
    }
}
