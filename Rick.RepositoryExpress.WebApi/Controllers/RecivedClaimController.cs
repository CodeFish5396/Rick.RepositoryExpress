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
    public class RecivedClaimController : RickControllerBase
    {
        private readonly ILogger<RecivedClaimController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public RecivedClaimController(ILogger<RecivedClaimController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询已到库订单
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RecivedClaimResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            await _expressclaimService.BeginTransactionAsync();
            var query = from expressclaim in _expressclaimService.Query<Expressclaim>(t => t.Status == (int)ExpressClaimStatus.已入库 && t.Appuser == UserInfo.Id)
                        join expressinfo in _expressclaimService.Query<Expressinfo>(t => 1 == 1)
                        on expressclaim.Expressinfoid equals expressinfo.Id
                        join package in _expressclaimService.Query<Package>(t => t.Status >= 1)
                        on expressclaim.Packageid equals package.Id
                        select new RecivedClaimResponse()
                        {
                            Id = expressclaim.Id,
                            PackageName = package.Name,
                            Weight = package.Weight,
                            Volume = package.Volume,
                            Expressnumber = expressinfo.Expressnumber,
                            CourierName = string.Empty,
                            CourierId = expressinfo.Courierid,
                            Addtime = package.Addtime
                        };
            var count = await query.CountAsync();
            var results = await (query.OrderByDescending(t=>t.Addtime).Skip((index - 1) * pageSize).Take(pageSize)).ToListAsync();
            await _expressclaimService.CommitAsync();

            RecivedClaimResponseList recivedClaimResponseList = new RecivedClaimResponseList();
            recivedClaimResponseList.List = results;
            recivedClaimResponseList.Count = count;
            return RickWebResult.Success(recivedClaimResponseList);
        }

        public class RecivedClaimResponse
        {
            public long Id { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public string CourierName { get; set; }
            public long? CourierId { get; set; }
            public DateTime Addtime { get; set; }
            public Decimal? Weight { get; set; }
            public Decimal? Volume { get; set; }

        }
        public class RecivedClaimResponseList
        {
            public int Count { get; set; }
            public IList<RecivedClaimResponse> List { get; set; }
        }

    }
}
