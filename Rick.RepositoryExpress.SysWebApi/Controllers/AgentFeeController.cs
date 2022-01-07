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
    /// 代理商成本
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentFeeController : RickControllerBase
    {
        private readonly ILogger<AgentFeeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentFeeService _agentFeeService;
        private readonly RedisClientService _redisClientService;
        /// <summary>
        /// 代理商成本会计科目
        /// </summary>
        private string accountSubjectId = "1475363832376463360";
        private string accountSubjectCode = "200";

        public AgentFeeController(ILogger<AgentFeeController> logger, IAgentFeeService agentFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentFeeService = agentFeeService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 新增代理商成本
        /// </summary>
        /// <param name="agentFeeRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] AgentFeeRequest agentFeeRequest)
        {
            await _agentFeeService.BeginTransactionAsync();
            Account account = new Account();
            DateTime now = DateTime.Now;
            account.Id = _idGenerator.NextId();
            account.Currencyid = agentFeeRequest.Currencyid;
            account.Amount = agentFeeRequest.Amount;
            account.Status = 1;
            account.Addtime = now;
            account.Adduser = UserInfo.Id;
            account.Subjectcode = accountSubjectCode;
            account.Direction = -1;
            await _agentFeeService.AddAsync(account);

            Agentfee agentfee = new Agentfee();
            agentfee.Id = _idGenerator.NextId();
            agentfee.Agentid = agentFeeRequest.Agentid;
            agentfee.Accountid = account.Id;
            agentfee.Status = 1;
            agentfee.Addtime = now;
            agentfee.Adduser = UserInfo.Id;
            agentfee.Paytype = agentFeeRequest.Paytype;
            await _agentFeeService.AddAsync(agentfee);

            await _agentFeeService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 查询代理商成本
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="currencyId"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AgentFeeResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] long? currencyId, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            AgentFeeResponseList agentFeeResponseList = new AgentFeeResponseList();

            var query = from account in _agentFeeService.Query<Account>(t => t.Subjectcode == accountSubjectCode && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        join agentfee in _agentFeeService.Query<Agentfee>()
                        on account.Id equals agentfee.Accountid
                        join currency in _agentFeeService.Query<Currency>(t => !currencyId.HasValue || t.Id == currencyId)
                        on account.Currencyid equals currency.Id
                        join agent in _agentFeeService.Query<Agent>()
                        on agentfee.Agentid equals agent.Id
                        join user in _agentFeeService.Query<Sysuser>()
                        on account.Adduser equals user.Id
                        select new AgentFeeResponse()
                        {
                            Id = account.Id,
                            Agentid = agentfee.Agentid,
                            Agentname = agent.Name,
                            Currencyid = account.Currencyid,
                            Currencyname = currency.Name,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addusername = user.Name,
                            Addtime = account.Addtime,
                            Paytype = agentfee.Paytype,
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by new { agentfeeresponse.Currencyid, agentfeeresponse.Currencyname }
                                  into sumItem
                                  select new AgentFeeResponseSum()
                                  {
                                      Currencyid = sumItem.Key.Currencyid,
                                      Currencyname = sumItem.Key.Currencyname,
                                      TotalAmount = sumItem.Sum()
                                  }).ToListAsync();

            agentFeeResponseList.SumList = sumQuery;
            agentFeeResponseList.Count = await query.CountAsync();
            agentFeeResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(agentFeeResponseList);
        }

        public class AgentFeeResponse
        {
            public long Id { get; set; }
            public long Agentid { get; set; }
            public string Agentname { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public int Paytype { get; set; }
        }
        public class AgentFeeResponseList
        {
            public int Count { get; set; }
            public List<AgentFeeResponse> List { get; set; }
            public List<AgentFeeResponseSum> SumList { get; set; }

        }
        public class AgentFeeResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }

        public class AgentFeeRequest
        {
            public long Agentid { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public int Paytype { get; set; }
        }
    }
}
