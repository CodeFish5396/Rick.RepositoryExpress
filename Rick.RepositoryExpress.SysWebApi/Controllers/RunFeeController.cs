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
    public class RunFeeController : RickControllerBase
    {
        private readonly ILogger<RunFeeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRunFeeService _runFeeService;
        private readonly RedisClientService _redisClientService;
        /// <summary>
        /// 运营成本成本
        /// </summary>
        private string accountSubjectId = "1475363978350825472";
        private string accountSubjectCode = "300";

        public RunFeeController(ILogger<RunFeeController> logger, IRunFeeService runFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _runFeeService = runFeeService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 新增运营成本
        /// </summary>
        /// <param name="agentFeeRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] RunFeeRequest agentFeeRequest)
        {
            await _runFeeService.BeginTransactionAsync();
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
            await _runFeeService.AddAsync(account);

            Runfee runfee = new Runfee();
            runfee.Id = _idGenerator.NextId();
            runfee.Name = agentFeeRequest.Name;
            runfee.Paytime = agentFeeRequest.Paytime;
            runfee.Accountid = account.Id;
            runfee.Status = 1;
            runfee.Addtime = now;
            runfee.Adduser = UserInfo.Id;
            await _runFeeService.AddAsync(runfee);

            await _runFeeService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 查询代理商成本
        /// </summary>
        /// <param name="currencyid"></param>
        /// <param name="name"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RunFeeResponseList>> Get([FromQuery] long? currencyid, [FromQuery] string name, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            RunFeeResponseList agentFeeResponseList = new RunFeeResponseList();

            var query = from account in _runFeeService.Query<Account>(t => t.Subjectcode == accountSubjectCode && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        join runfee in _runFeeService.Query<Runfee>()
                        on account.Id equals runfee.Accountid
                        join currency in _runFeeService.Query<Currency>(t=> !currencyid.HasValue || t.Id == currencyid)
                        on account.Currencyid equals currency.Id
                        join user in _runFeeService.Query<Sysuser>(t=>string.IsNullOrEmpty(name) || t.Name == name)
                        on account.Adduser equals user.Id
                        select new RunFeeResponse()
                        {
                            Id = account.Id,
                            Name = runfee.Name,
                            Currencyid = account.Currencyid,
                            Currencyname = currency.Name,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addusername = user.Name,
                            Addtime = account.Addtime,
                            Paytime = runfee.Paytime
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by new { agentfeeresponse.Currencyid, agentfeeresponse.Currencyname }
                                  into sumItem
                                  select new RunFeeRequestSum()
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

        public class RunFeeResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime Paytime { get; set; }
        }
        public class RunFeeResponseList
        {
            public int Count { get; set; }
            public List<RunFeeResponse> List { get; set; }
            public List<RunFeeRequestSum> SumList { get; set; }
        }
        public class RunFeeRequestSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }

        public class RunFeeRequest
        {
            public string Name { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public DateTime Paytime { get; set; }

        }

    }
}
