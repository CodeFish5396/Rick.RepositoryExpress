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
    public class HomePageController : RickControllerBase
    {
        private readonly ILogger<HomePageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public HomePageController(ILogger<HomePageController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 首页数据查询，未到库、已到库、订单数查询
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<HomePageResponse>> Get()
        {
            HomePageResponse homePageResponse = new HomePageResponse();
            var queryRecievedCount = from expressclaim in _sysuserService.Query<Expressclaim>(t => t.Status == (int)ExpressClaimStatus.已揽收 && t.Appuser == UserInfo.Id)
                        select expressclaim.Id;

            homePageResponse.RecievedCount = await queryRecievedCount.CountAsync();

            var queryUnrecievedCount = from expressclaim in _sysuserService.Query<Expressclaim>(t => (t.Status == (int)ExpressClaimStatus.预报 || t.Status == (int)ExpressClaimStatus.已入库) && t.Appuser == UserInfo.Id)
                        select expressclaim.Id;

            homePageResponse.UnrecievedCount = await queryUnrecievedCount.CountAsync();

            //var queryOrderCount = from packageorderapply in _sysuserService.Query<Packageorderapply>(t => t.Status == 1 && t.Appuser == UserInfo.Id)
            //            select packageorderapply.Id;
            //homePageResponse.OrderCount = await queryOrderCount.CountAsync();

            var queryErrorCount = from Packageorderapplyerror in _sysuserService.Query<Packageorderapplyerror>(t => t.Appuser == UserInfo.Id)
                                  select Packageorderapplyerror.Id;
            homePageResponse.ErrorCount = await queryErrorCount.CountAsync();

            return RickWebResult.Success(homePageResponse);
        }
        public class HomePageResponse
        { 
            public int RecievedCount { get; set; }
            public int UnrecievedCount { get; set; }
            public int OrderCount { get; set; }
            public int ErrorCount { get; set; }

        }
    }
}
