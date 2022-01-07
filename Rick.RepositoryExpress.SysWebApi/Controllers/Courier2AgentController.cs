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
    /// 快递公司
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class Courier2AgentController : RickControllerBase
    {
        private readonly ILogger<Courier2AgentController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ICourierService _courierService;
        private readonly RedisClientService _redisClientService;

        public Courier2AgentController(ILogger<Courier2AgentController> logger, ICourierService courierService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _courierService = courierService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 通过代理商查询快递公司
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<CourierResponse>>> Get([FromQuery] long? agentId)
        {
            var agent2couriers = await _courierService.QueryAsync<Agentandcourier>(t => (!agentId.HasValue || t.Agentid == agentId) && t.Status == 1);
            var courierids = agent2couriers.Select(t => t.Courierid).ToList();

            var results = await _courierService.QueryAsync<Courier>(t => courierids.Contains(t.Id) && t.Status == 1);
           
            return RickWebResult.Success(results.Select(t => new CourierResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Extname = t.Extname,
                Status = t.Status,
                Addtime = t.Addtime
            }));
        }
    }


}
