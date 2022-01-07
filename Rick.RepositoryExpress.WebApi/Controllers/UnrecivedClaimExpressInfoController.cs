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
using Rick.RepositoryExpress.Utils.ExpressApi;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnrecivedClaimExpressInfoController : RickControllerBase
    {
        private readonly ILogger<UnrecivedClaimExpressInfoController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public UnrecivedClaimExpressInfoController(ILogger<UnrecivedClaimExpressInfoController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询国内快递详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<List<UnrecivedClaimExpressInfoResponse>>> Get([FromQuery] long id)
        {
            List<UnrecivedClaimExpressInfoResponse> results = new List<UnrecivedClaimExpressInfoResponse>();
            var expressinfo = await _packageOrderApplyService.FindAsync<Expressinfo>(id);
            var expressinfostatuses = (await _packageOrderApplyService.QueryAsync<Expressinfostatus>(t => t.Expressinfoid == expressinfo.Id)).OrderByDescending(t => t.Addtime).ToList();
            if (expressinfostatuses == null || expressinfostatuses.Count == 0 || expressinfostatuses[0].Searchtime <= DateTime.Now.AddHours(-6))
            {
                var courier = await _packageOrderApplyService.FindAsync<Courier>((long)expressinfo.Courierid);
                string expressStatus = await ExpressApiHelper.Get(expressinfo.Expressnumber, courier.Code);

                UnrecivedClaimExpressInfoApiExpressStatus apiExpressStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<UnrecivedClaimExpressInfoApiExpressStatus>(expressStatus);

                if (apiExpressStatus != null && apiExpressStatus.Success && apiExpressStatus.Traces != null && apiExpressStatus.Traces.Count > 0)
                {
                    foreach (var trace in apiExpressStatus.Traces)
                    {
                        UnrecivedClaimExpressInfoResponse orderApplyExpressStatusResponse = new UnrecivedClaimExpressInfoResponse();
                        orderApplyExpressStatusResponse.Location = trace.AcceptStation;
                        orderApplyExpressStatusResponse.Addtime = trace.AcceptTime;
                        results.Add(orderApplyExpressStatusResponse);
                    }
                }
                if (results.Count > 0)
                {
                    await _packageOrderApplyService.BeginTransactionAsync();
                    foreach (var expressinfostatus in expressinfostatuses)
                    {
                        await _packageOrderApplyService.DeleteAsync<Expressinfostatus>(expressinfostatus.Id);
                    }
                    foreach (var result in results)
                    {
                        Expressinfostatus expressinfostatus = new Expressinfostatus();
                        expressinfostatus.Id = _idGenerator.NextId();
                        expressinfostatus.Location = result.Location;
                        expressinfostatus.Addtime = result.Addtime;
                        expressinfostatus.Expressinfoid = expressinfo.Id;
                        expressinfostatus.Searchtime = DateTime.Now;
                        await _packageOrderApplyService.AddAsync(expressinfostatus);
                    }
                    await _packageOrderApplyService.CommitAsync();
                }
                results = results.OrderByDescending(t => t.Addtime).ToList();
            }
            else
            {
                results = expressinfostatuses.Select(t => new UnrecivedClaimExpressInfoResponse()
                {
                    Location = t.Location,
                    Addtime = t.Addtime,
                }).OrderByDescending(t=>t.Addtime).ToList();
            }

            return RickWebResult.Success(results);
        }

        public class UnrecivedClaimExpressInfoResponse
        {
            public DateTime Addtime { get; set; }
            public string Location { get; set; }
        }

        public class UnrecivedClaimExpressInfoApiExpressStatus
        {
            public string LogisticCode { get; set; }
            public string ShipperCode { get; set; }
            public List<UnrecivedClaimExpressInfoApiExpressStatusDetail> Traces { get; set; }
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
        public class UnrecivedClaimExpressInfoApiExpressStatusDetail
        {
            public string AcceptStation { get; set; }
            public DateTime AcceptTime { get; set; }


        }


    }
}
