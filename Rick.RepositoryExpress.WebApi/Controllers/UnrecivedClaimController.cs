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
    public class UnrecivedClaimController : RickControllerBase
    {
        private readonly ILogger<UnrecivedClaimController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public UnrecivedClaimController(ILogger<UnrecivedClaimController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询未到库订单
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UnrecivedClaimResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            await _expressclaimService.BeginTransactionAsync();
            var query = from expressclaim in _expressclaimService.Query<Expressclaim>(t => (t.Status == (int)ExpressClaimStatus.正常 || t.Status == (int)ExpressClaimStatus.已到库) && t.Appuser == UserInfo.Id)
                        join expressinfo in _expressclaimService.Query<Expressinfo>(t => 1 == 1)
                        on expressclaim.Expressinfoid equals expressinfo.Id
                        select new UnrecivedClaimResponse()
                        {
                            Expressnumber = expressinfo.Expressnumber,
                            CourierName = string.Empty,
                            CourierId = expressinfo.Courierid,
                            Addtime = expressclaim.Addtime
                        };
            var count = await query.CountAsync();
            var results = await (query.OrderByDescending(t=>t.Addtime).Skip((index - 1) * pageSize).Take(pageSize)).ToListAsync();
            await _expressclaimService.CommitAsync();

            UnrecivedClaimResponseList unrecivedClaimResponseList = new UnrecivedClaimResponseList();
            unrecivedClaimResponseList.List = results;
            unrecivedClaimResponseList.Count = count;
            return RickWebResult.Success(unrecivedClaimResponseList);
        }

        public class UnrecivedClaimResponse
        {
            public string Expressnumber { get; set; }
            public string CourierName { get; set; }
            public long? CourierId { get; set; }
            public DateTime Addtime { get; set; }
        }
        public class UnrecivedClaimResponseList
        {
            public int Count { get; set; }
            public IList<UnrecivedClaimResponse> List { get; set; }
        }

    }
}
