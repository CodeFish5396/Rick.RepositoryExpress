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
using System.IO;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
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
        /// 获取首页概览统计
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<HomePageResponse>> Get()
        {
            HomePageResponse homePageResponse = new HomePageResponse();
            await _sysuserService.BeginTransactionAsync();
            var queryUser = _sysuserService.Query<Appuser>(t => t.Status == 1);

            var queryClaim = _sysuserService.Query<Expressclaim>(t => t.Status >= 1);

            var queryOrder = _sysuserService.Query<Packageorderapply>(t=>t.Status == 1 && t.Orderstatus >= 1 && t.Orderstatus != (int)OrderApplyStatus.问题件);

            var queryPackage = _sysuserService.Query<Package>(t => t.Status >= 1);

            DateTime now = DateTime.Now;
            DateTime dateTimeBegin = new DateTime(now.Year, now.Month, now.Day);
            DateTime dateTimeEnd = new DateTime(now.Year, now.Month, now.Day, 12, 59, 59);
            homePageResponse.TodayUserCount = await queryUser.CountAsync(t=>t.Addtime >= dateTimeBegin && t.Addtime <= dateTimeEnd);
            homePageResponse.UserCount = await queryUser.CountAsync();

            homePageResponse.TodayClaimCount = await queryClaim.CountAsync(t => t.Addtime >= dateTimeBegin && t.Addtime <= dateTimeEnd);
            homePageResponse.ClaimCount = await queryClaim.CountAsync();

            homePageResponse.TodaySendCount = await queryOrder.CountAsync(t => t.Orderstatus >= (int)OrderApplyStatus.已发货 && t.Sendtime >= dateTimeBegin && t.Sendtime <= dateTimeEnd);
            homePageResponse.SendCount = await queryOrder.CountAsync(t => t.Orderstatus >= (int)OrderApplyStatus.已发货);

            homePageResponse.TodayCheckCount = await queryOrder.CountAsync(t => t.Orderstatus >= (int)OrderApplyStatus.已签收 && t.Sendtime >= dateTimeBegin && t.Sendtime <= dateTimeEnd);
            homePageResponse.CheckCount = await queryOrder.CountAsync(t => t.Orderstatus >= (int)OrderApplyStatus.已签收);


            homePageResponse.TodayToSendCount = await queryOrder.CountAsync(t => t.Orderstatus == (int)OrderApplyStatus.待发货 && t.Lasttime >= dateTimeBegin && t.Lasttime <= dateTimeEnd);
            homePageResponse.ToSendCount = await queryOrder.CountAsync(t => t.Orderstatus == (int)OrderApplyStatus.待发货);

            homePageResponse.TodayPackageCount = await queryPackage.CountAsync(t => t.Status >= (int)PackageStatus.已入库 && t.Addtime >= dateTimeBegin && t.Addtime <= dateTimeEnd);
            homePageResponse.PackageCount = await queryPackage.CountAsync(t => t.Status >= (int)PackageStatus.已入库);

            homePageResponse.TodayRepositoryCount = await queryPackage.CountAsync(t => t.Status >= (int)PackageStatus.已入柜 && t.Repositoryintime >= dateTimeBegin && t.Repositoryintime <= dateTimeEnd);
            homePageResponse.RepositoryCount = await queryPackage.CountAsync(t => t.Status >= (int)PackageStatus.已入柜);
            homePageResponse.CurrentRepositoryCount = await queryPackage.CountAsync(t => t.Status >= (int)PackageStatus.已入柜 && t.Status != (int)PackageStatus.已出库);

            return RickWebResult.Success<HomePageResponse>(homePageResponse) ;
        }

        public class HomePageResponse
        {
            public int TodayUserCount { get; set; }
            public int UserCount { get; set; }
            public int TodayClaimCount { get; set; }
            public int ClaimCount { get; set; }
            public int TodaySendCount { get; set; }
            public int SendCount { get; set; }
            public int TodayCheckCount { get; set; }
            public int CheckCount { get; set; }
            public int TodayToSendCount { get; set; }
            public int ToSendCount { get; set; }
            public int TodayPackageCount { get; set; }
            public int PackageCount { get; set; }
            public int TodayRepositoryCount { get; set; }
            public int RepositoryCount { get; set; }
            public int CurrentRepositoryCount { get; set; }
        }


    }
}
