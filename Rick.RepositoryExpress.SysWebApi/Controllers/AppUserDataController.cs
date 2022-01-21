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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// APP用户交易查询
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AppUserDataController : RickControllerBase
    {
        private readonly ILogger<AppUserDataController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;
        private readonly RedisClientService _redisClientService;

        public AppUserDataController(ILogger<AppUserDataController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuserService = appuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// APP用户交易查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppUserDataResponseList>> Get([FromQuery] long id, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            //查询用户余额、充值、交易情况
            AppUserDataResponseList appUserResponseList = new AppUserDataResponseList();
            Appuser appuser = await _appuserService.FindAsync<Appuser>(id);

            appUserResponseList.Id = appuser.Id;
            appUserResponseList.Usercode = appuser.Usercode;

            var query = from viewappuserdatum in _appuserService.Query<Viewappuserdatum>(t => t.Appuser == appuser.Id)
                        select new AppUserDataResponse()
                        {
                            Id = viewappuserdatum.Id,
                            Type = viewappuserdatum.Type,
                            Currencyid = (long)viewappuserdatum.Currencyid,
                            Amount = viewappuserdatum.Amount,
                            Adduser = viewappuserdatum.Adduser,
                            Appuser = viewappuserdatum.Appuser,
                            Addtime = viewappuserdatum.Addtime,
                            Orderid = viewappuserdatum.Orderid ?? 0,
                            Ordercode = string.Empty,
                            Paytype = viewappuserdatum.Paytype
                        };
            appUserResponseList.Count = await query.CountAsync();
            appUserResponseList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            var sumQuery = from viewappuserdatum in query
                           group new { viewappuserdatum.Type, viewappuserdatum.Amount } by viewappuserdatum.Currencyid
                           into viewappuserdatumGT
                           select new AppUserDataAccountResponse()
                           {
                               Currencyid = viewappuserdatumGT.Key,
                               Chargeamount = viewappuserdatumGT.Where(t => t.Type == 1).Sum(t => t.Amount),
                               Consumeamount = viewappuserdatumGT.Where(t => t.Type == -1).Sum(t => t.Amount)
                           };
            appUserResponseList.Accounts = await sumQuery.ToListAsync();
            var orderids = appUserResponseList.List.Where(t => t.Orderid > 0).Select(t => t.Orderid).ToList();
            if (orderids != null && orderids.Count > 0)
            {
                var orders = await _appuserService.QueryAsync<Packageorderapply>(order => orderids.Contains(order.Id));
                foreach (AppUserDataResponse appUserDataResponse in appUserResponseList.List)
                {
                    if (appUserDataResponse.Orderid > 0)
                    {
                        appUserDataResponse.Ordercode = orders.Single(t => t.Id == appUserDataResponse.Orderid).Code;
                    }
                }
            }

            var currencyIds = appUserResponseList.List.Select(t => t.Currencyid).ToList();
            currencyIds.AddRange(appUserResponseList.Accounts.Select(t => t.Currencyid));

            var currencies = await _appuserService.QueryAsync<Currency>(c => currencyIds.Contains(c.Id));
            foreach (AppUserDataResponse appUserDataResponse in appUserResponseList.List)
            {
                appUserDataResponse.Currencyname = currencies.Single(t => t.Id == appUserDataResponse.Currencyid).Name;
            }
            foreach (AppUserDataAccountResponse appUserDataAccountResponse in appUserResponseList.Accounts)
            {
                appUserDataAccountResponse.Currencyname = currencies.Single(t => t.Id == appUserDataAccountResponse.Currencyid).Name;
            }

            return RickWebResult.Success(appUserResponseList);
        }

        public class AppUserDataResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public long Type { get; set; }
            public decimal Amount { get; set; }
            public long Appuser { get; set; }
            public long Adduser { get; set; }
            public DateTime Addtime { get; set; }
            public long Paytype { get; set; }
            public long Orderid { get; set; }
            public string Ordercode { get; set; }
        }

        public class AppUserDataAccountResponse
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Chargeamount { get; set; }
            public decimal Consumeamount { get; set; }
        }

        public class AppUserDataResponseList
        {
            public long Id { get; set; }
            public string Usercode { get; set; }
            public List<AppUserDataAccountResponse> Accounts { get; set; }
            public int Count { get; set; }
            public List<AppUserDataResponse> List { get; set; }
        }

    }
}
