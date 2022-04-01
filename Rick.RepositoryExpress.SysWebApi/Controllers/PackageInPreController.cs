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
using Rick.RepositoryExpress.DataBase.ViewModels;
using Rick.RepositoryExpress.Utils.ExpressApi;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 快递扫描
    /// </summary>
    [Route("api/[controller]/")]
    [ApiController]
    public class PackageInPreController : RickControllerBase
    {
        private readonly ILogger<PackageInPreController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public PackageInPreController(ILogger<PackageInPreController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 包裹入库，扫码查询
        /// </summary>
        /// <param name="expressnumber"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<PackageInPreResponse>> Get([FromQuery] string expressnumber)
        {
            PackageInPreResponse packageInPreResponse = new PackageInPreResponse();
            packageInPreResponse.details = new List<PackageInPreDetailView>();
            packageInPreResponse.Expressnumber = expressnumber;
            //TO-DO 获取快递产商
            if (expressnumber.StartsWith("sf") || expressnumber.StartsWith("SF"))
            {
                packageInPreResponse.CourierId = 1477581753219682304;
                packageInPreResponse.CourierCode = "SF";
                packageInPreResponse.CourierName = "顺丰快递";
            }
            else
            {
                string expressStatus = await ExpressApiHelper.Get(expressnumber);
                ApiExpressStatus apiExpressStatus = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiExpressStatus>(expressStatus);
                if (apiExpressStatus != null && apiExpressStatus.Success && apiExpressStatus.Traces != null && apiExpressStatus.Traces.Count > 0)
                {
                    var couries = await _packageService.QueryAsync<Courier>(t=>t.Code == apiExpressStatus.ShipperCode);
                    if (couries != null && couries.Count > 0)
                    {
                        var courier = couries[0];
                        packageInPreResponse.CourierId = courier.Id;
                        packageInPreResponse.CourierCode = courier.Code;
                        packageInPreResponse.CourierName = courier.Name;
                    }
                }
            }

            //获取包裹预报的用户
            var users = await _packageService.GetAppusers(expressnumber);
            packageInPreResponse.details = users;

            return RickWebResult.Success(packageInPreResponse);

        }

    }

    public class PackageInPreRequest
    {
        public string Expressnumber { get; set; }
    }

    public class PackageInPreResponse
    {
        public string Expressnumber { get; set; }
        public long CourierId { get; set; }
        public string CourierName { get; set; }
        public string CourierCode { get; set; }
        public IList<PackageInPreDetailView> details { get; set; }
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
