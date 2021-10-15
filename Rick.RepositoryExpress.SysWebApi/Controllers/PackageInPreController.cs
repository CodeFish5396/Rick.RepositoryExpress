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
        public IList<PackageInPreDetailView> details { get; set; }
    }

}
