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
using Rick.RepositoryExpress.Utils.ExpressApi;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApplyExpressStatusController : RickControllerBase
    {
        private readonly ILogger<OrderApplyExpressStatusController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public OrderApplyExpressStatusController(ILogger<OrderApplyExpressStatusController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询快递详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<List<OrderApplyExpressStatusResponse>>> Get([FromQuery] long id)
        {
            List<OrderApplyExpressStatusResponse> results = new List<OrderApplyExpressStatusResponse>();
            var packageorderapplyexpress = await _packageOrderApplyService.FindAsync<Packageorderapplyexpress>(id);
            var packageorderapplyexpressstatuses = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpressstatus>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id)).OrderByDescending(t => t.Addtime).ToList();
            if (packageorderapplyexpressstatuses == null || packageorderapplyexpressstatuses.Count == 0 || packageorderapplyexpressstatuses[0].Searchtime <= DateTime.Now.AddHours(-6))
            {
                string outNumber = packageorderapplyexpress.Outnumber;
                if (outNumber.StartsWith("sf") || outNumber.StartsWith("SF"))
                {
                    outNumber += ":5128";
                }
                string expressStatus = await ExpressApiHelper.Get(outNumber, packageorderapplyexpress.Couriercode);

                ApiExpressStatus apiExpressStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiExpressStatus>(expressStatus);

                if (apiExpressStatus != null && apiExpressStatus.Success && apiExpressStatus.Traces != null && apiExpressStatus.Traces.Count > 0)
                {
                    foreach (var trace in apiExpressStatus.Traces)
                    {
                        OrderApplyExpressStatusResponse orderApplyExpressStatusResponse = new OrderApplyExpressStatusResponse();
                        orderApplyExpressStatusResponse.Location = trace.AcceptStation;
                        orderApplyExpressStatusResponse.Addtime = trace.AcceptTime;
                        results.Add(orderApplyExpressStatusResponse);
                    }
                }
                if (results.Count > 0)
                {
                    await _packageOrderApplyService.BeginTransactionAsync();
                    foreach (var packageorderapplyexpressstatus in packageorderapplyexpressstatuses)
                    {
                        await _packageOrderApplyService.DeleteAsync<Packageorderapplyexpressstatus>(packageorderapplyexpressstatus.Id);
                    }
                    foreach (var result in results)
                    {
                        Packageorderapplyexpressstatus packageorderapplyexpressstatus = new Packageorderapplyexpressstatus();
                        packageorderapplyexpressstatus.Id = _idGenerator.NextId();
                        packageorderapplyexpressstatus.Location = result.Location;
                        packageorderapplyexpressstatus.Addtime = result.Addtime;
                        packageorderapplyexpressstatus.Packageorderapplyexpressid = id;
                        packageorderapplyexpressstatus.Searchtime = DateTime.Now;
                        await _packageOrderApplyService.AddAsync<Packageorderapplyexpressstatus>(packageorderapplyexpressstatus);
                    }

                    await _packageOrderApplyService.CommitAsync();
                }
            }
            else
            {
                results = packageorderapplyexpressstatuses.Select(t => new OrderApplyExpressStatusResponse()
                {
                    Location = t.Location,
                    Addtime = t.Addtime,
                }).ToList();
            }
            return RickWebResult.Success(results);
        }

        public class OrderApplyExpressStatusResponse
        {
            public DateTime Addtime { get; set; }
            public string Location { get; set; }
        }

        public class ApiExpressStatus
        {
            public string LogisticCode { get; set; }
            public string ShipperCode { get; set; }
            public List<ApiExpressStatusDetail> Traces { get; set; }
            public string State { get; set; }
            public bool Success { get; set; }
            public string Courier { get; set; }
            public string CourierPhone { get; set; }
            public string updateTime { get; set; }
            public string takeTime { get; set; }
            public string Name { get; set; }
            public string Site { get; set; }
            public string Phone { get; set; }
            public string Logo { get; set; }
            public string Reason { get; set; }

        }
        public class ApiExpressStatusDetail
        {
            public string AcceptStation { get; set; }
            public DateTime AcceptTime { get; set; }


        }


    }
}
