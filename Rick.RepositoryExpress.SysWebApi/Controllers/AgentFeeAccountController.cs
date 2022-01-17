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
    /// 代理商账户明细
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentFeeAccountController : RickControllerBase
    {
        private readonly ILogger<AgentFeeAccountController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentFeeService _agentFeeService;
        private readonly RedisClientService _redisClientService;
        /// <summary>
        /// 代理商成本会计科目
        /// </summary>
        private string accountSubjectId = "1475363832376463360";
        private string accountSubjectCode = "200";

        public AgentFeeAccountController(ILogger<AgentFeeAccountController> logger, IAgentFeeService agentFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentFeeService = agentFeeService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询代理商账户
        /// </summary>
        /// <param name="agentId"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AgentFeeAccountResponseList>> Get([FromQuery] long? agentId, [FromQuery] long? currencyId)
        {
            AgentFeeAccountResponseList agentFeeResponseList = new AgentFeeAccountResponseList();

            var query = from account in _agentFeeService.Query<Agentfeeaccount>(t => t.Status == 1 && (!agentId.HasValue || t.Agentid == agentId) && (!currencyId.HasValue || t.Agentid == currencyId))
                        join currency in _agentFeeService.Query<Currency>()
                        on account.Currencyid equals currency.Id
                        join agent in _agentFeeService.Query<Agent>()
                        on account.Agentid equals agent.Id
                        select new AgentFeeAccountResponse()
                        {
                            Id = account.Id,
                            Currencyid = account.Currencyid,
                            Currencyname = currency.Name,
                            Agentid = agent.Id,
                            Agentname = agent.Name,
                            Amount = account.Amount
                        };
            agentFeeResponseList.List = await query.ToListAsync();
            agentFeeResponseList.Count = agentFeeResponseList.List.Count;
            return RickWebResult.Success(agentFeeResponseList);
        }

        public class AgentFeeAccountResponse
        {
            public long Id { get; set; }
            public long Agentid { get; set; }
            public string Agentname { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
        }
        public class AgentFeeAccountResponseList
        {
            public int Count { get; set; }
            public List<AgentFeeAccountResponse> List { get; set; }

        }

    }
}
