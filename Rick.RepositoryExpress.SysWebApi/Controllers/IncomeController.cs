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
    [Route("api/[controller]")]
    [ApiController]
    public class IncomeController : RickControllerBase
    {
        private readonly ILogger<IncomeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IIncomeService _incomeService;
        private readonly RedisClientService _redisClientService;

        public IncomeController(ILogger<IncomeController> logger, IIncomeService incomeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _incomeService = incomeService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户充值
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="currencyId"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IncomeResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] long? currencyId, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            IncomeResponseList incomeRequestList = new IncomeResponseList();

            var query = from charge in _incomeService.Query<Appuseraccountcharge>(t => t.Status == 1 && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime) && (!currencyId.HasValue || t.Currencyid == currencyId))
                        join account in _incomeService.Query<Account>()
                        on charge.Accountid equals account.Id
                        select new IncomeResponse()
                        {
                            Id = charge.Id,
                            Appuser = charge.Appuser,
                            Amount = charge.Amount,
                            Currencyid = charge.Currencyid,
                            Addtime = charge.Addtime,
                            Adduser = account.Adduser,
                            Paytype = charge.Paytype
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by agentfeeresponse.Currencyid
                           into sumItem
                                  select new IncomeResponseSum()
                                  {
                                      Currencyid = sumItem.Key,
                                      TotalAmount = sumItem.Sum()
                                  }).ToListAsync();

            incomeRequestList.SumList = sumQuery;
            List<long> sumcurrencyids = incomeRequestList.SumList.Select(t => t.Currencyid).Distinct().ToList();
            List<Currency> sumcurrencies = await (from c in _incomeService.Query<Currency>(t => sumcurrencyids.Contains(t.Id)) select c).ToListAsync();
            foreach (var incomeResponse in incomeRequestList.SumList)
            {
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.Currencyname = ccurrency.Name;
            }

            incomeRequestList.Count = await query.CountAsync();
            incomeRequestList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            List<long> users = incomeRequestList.List.Select(t => t.Appuser).Distinct().ToList();
            //List<long> currencyids = incomeRequestList.List.Select(t => t.Currencyid).Distinct().ToList();
            List<Appuser> appusers = await (from c in _incomeService.Query<Appuser>(t => users.Contains(t.Id)) select c).ToListAsync();
            //List<Currency> currencies = await (from c in _incomeService.Query<Currency>(t => currencyids.Contains(t.Id)) select c).ToListAsync();

            List<long> sysuserids = incomeRequestList.List.Select(t => t.Adduser).Distinct().ToList();
            List<Sysuser> sysusers = await (from c in _incomeService.Query<Sysuser>(t => sysuserids.Contains(t.Id)) select c).ToListAsync();

            foreach (var incomeResponse in incomeRequestList.List)
            {
                var cuser = appusers.SingleOrDefault(t => t.Id == incomeResponse.Appuser);
                incomeResponse.UserName = cuser.Name;
                incomeResponse.UserMobil = cuser.Mobile;
                incomeResponse.Usercode = cuser.Usercode;
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.CurrencyName = ccurrency.Name;
                var csysuser = sysusers.SingleOrDefault(t => t.Id == incomeResponse.Adduser);
                incomeResponse.Addusername = csysuser.Name;
            }

            return RickWebResult.Success(incomeRequestList);
        }
        public class IncomeResponse
        {
            public long Id { get; set; }
            public long Appuser { get; set; }
            public string UserName { get; set; }
            public string UserMobil { get; set; }
            public string Usercode { get; set; }
            public decimal Amount { get; set; }
            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public DateTime Addtime { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public int Paytype { get; set; }

        }
        public class IncomeResponseList
        {
            public int Count { get; set; }
            public List<IncomeResponse> List { get; set; }
            public List<IncomeResponseSum> SumList { get; set; }
        }

        public class IncomeResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }


    }
}
