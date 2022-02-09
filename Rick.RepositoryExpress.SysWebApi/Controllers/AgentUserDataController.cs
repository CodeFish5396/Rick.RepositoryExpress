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
    /// 代理商交易查询
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentUserDataController : RickControllerBase
    {
        private readonly ILogger<AgentUserDataController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentService _agentService;
        private readonly RedisClientService _redisClientService;

        public AgentUserDataController(ILogger<AgentUserDataController> logger, IAgentService agentService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentService = agentService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 代理商交易查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AgentDataResponseList>> Get([FromQuery] long id, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            //查询用户余额、充值、交易情况
            AgentDataResponseList appUserResponseList = new AgentDataResponseList();
            Agent agent = await _agentService.FindAsync<Agent>(id);

            appUserResponseList.Id = agent.Id;
            appUserResponseList.Name = agent.Name;

            var query = from viewagentuserdatum in _agentService.Query<Viewagentuserdatum>(t => t.Agentid == agent.Id)
                        select new AgentDataResponse()
                        {
                            Id = viewagentuserdatum.Id,
                            Type = viewagentuserdatum.Type,
                            Currencyid = viewagentuserdatum.Currencyid,
                            Amount = viewagentuserdatum.Amount,
                            Adduser = viewagentuserdatum.Adduser,
                            Addtime = viewagentuserdatum.Addtime,
                            Orderid = viewagentuserdatum.Orderid,
                            Ordercode = string.Empty,
                            Paytype = viewagentuserdatum.Paytype
                        };
            appUserResponseList.Count = await query.CountAsync();
            appUserResponseList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            var sumQuery = from viewagentuserdatum in query
                           group new { viewagentuserdatum.Type, viewagentuserdatum.Amount } by viewagentuserdatum.Currencyid
               into viewappuserdatumGT
                           select new AgentDataAccountResponse()
                           {
                               Currencyid = viewappuserdatumGT.Key,
                               Chargeamount = viewappuserdatumGT.Where(t => t.Type == 1).Sum(t => t.Amount),
                               Consumeamount = viewappuserdatumGT.Where(t => t.Type == -1).Sum(t => t.Amount)
                           };

            appUserResponseList.Accounts = await sumQuery.ToListAsync();

            var orderids = appUserResponseList.List.Where(t => t.Orderid > 0).Select(t => t.Orderid).ToList();
            if (orderids != null && orderids.Count > 0)
            {
                var orders = await _agentService.QueryAsync<Packageorderapply>(order => orderids.Contains(order.Id));
                foreach (AgentDataResponse appUserDataResponse in appUserResponseList.List)
                {
                    if (appUserDataResponse.Orderid > 0)
                    {
                        appUserDataResponse.Ordercode = orders.Single(t => t.Id == appUserDataResponse.Orderid).Code;
                    }
                }
            }

            var currencyIds = appUserResponseList.List.Select(t => t.Currencyid).ToList();
            currencyIds.AddRange(appUserResponseList.Accounts.Select(t => t.Currencyid));

            var currencies = await _agentService.QueryAsync<Currency>(c => currencyIds.Contains(c.Id));
            foreach (AgentDataResponse appUserDataResponse in appUserResponseList.List)
            {
                appUserDataResponse.Currencyname = currencies.Single(t => t.Id == appUserDataResponse.Currencyid).Name;
            }
            foreach (AgentDataAccountResponse appUserDataAccountResponse in appUserResponseList.Accounts)
            {
                appUserDataAccountResponse.Currencyname = currencies.Single(t => t.Id == appUserDataAccountResponse.Currencyid).Name;
            }

            return RickWebResult.Success(appUserResponseList);
        }

        public class AgentDataResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public long Type { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public DateTime Addtime { get; set; }
            public long Paytype { get; set; }
            public long Orderid { get; set; }
            public string Ordercode { get; set; }
        }

        public class AgentDataAccountResponse
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Chargeamount { get; set; }
            public decimal Consumeamount { get; set; }
        }

        public class AgentDataResponseList
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<AgentDataAccountResponse> Accounts { get; set; }
            public int Count { get; set; }
            public List<AgentDataResponse> List { get; set; }
        }

    }
}
