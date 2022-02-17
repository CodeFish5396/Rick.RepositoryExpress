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
    /// 收支核算
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProfitController : RickControllerBase
    {
        private readonly ILogger<ProfitController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAccountsubjectService _accountsubjectService;
        private readonly RedisClientService _redisClientService;
        public ProfitController(ILogger<ProfitController> logger, IAccountsubjectService accountsubjectService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _accountsubjectService = accountsubjectService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询收支核算
        /// </summary>
        /// <param name="subjectcode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<ProfitResponseList>> Get([FromQuery] string subjectcode, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            ProfitResponseList profitResponseList = new ProfitResponseList();

            var query = from account in _accountsubjectService.Query<Account>(t => (string.IsNullOrEmpty(subjectcode) || t.Subjectcode == subjectcode) && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        select new ProfitResponse()
                        {
                            Id = account.Id,
                            Currencyid = account.Currencyid,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addtime = account.Addtime,
                            SubjectCode = account.Subjectcode,
                            Direction = account.Direction
                        };
            profitResponseList.Count = await query.CountAsync();
            profitResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();


            var sumQuery = await (from agentfeeresponse in query
                                  group new { agentfeeresponse.Amount, agentfeeresponse.Direction } by new { agentfeeresponse.Currencyid, agentfeeresponse.Direction }
                                  into sumItem
                                  select new ProfitResponseSum()
                                  {
                                      Currencyid = sumItem.Key.Currencyid,
                                      Direction = sumItem.Key.Direction,
                                      TotalAmount = sumItem.Sum(t => t.Amount)
                                  }).ToListAsync();

            profitResponseList.SumList = sumQuery;

            List<long> sumcurrencyids = profitResponseList.SumList.Select(t => t.Currencyid).Distinct().ToList();
            List<Currency> sumcurrencies = await (from c in _accountsubjectService.Query<Currency>(t => sumcurrencyids.Contains(t.Id)) select c).ToListAsync();
            foreach (var incomeResponse in profitResponseList.SumList)
            {
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.Currencyname = ccurrency.Name;
            }
            profitResponseList.InList = profitResponseList.SumList.Where(t => t.Direction == 1).ToList();
            profitResponseList.OutList = profitResponseList.SumList.Where(t => t.Direction == -1).ToList();
            profitResponseList.ProfitList = (from profit in profitResponseList.SumList
                                             group new { profit.TotalAmount, profit.Direction } by new { profit.Currencyid, profit.Currencyname }
                                            into profitGT
                                             select new ProfitResponseSum()
                                             {
                                                 Currencyid = profitGT.Key.Currencyid,
                                                 Currencyname = profitGT.Key.Currencyname,
                                                 Direction = 0,
                                                 TotalAmount = profitGT.Sum(t => t.TotalAmount * t.Direction)
                                             }).ToList();
            //收入
            var incomeAccountIds = profitResponseList.List.Where(t => t.SubjectCode == "100").Select(t => t.Id).ToList();
            var appusers = await (from charge in _accountsubjectService.Query<Appuseraccountcharge>(t => incomeAccountIds.Contains(t.Accountid))
                                  join appuser in _accountsubjectService.Query<Appuser>()
                                  on charge.Appuser equals appuser.Id
                                  select new
                                  {
                                      Chargeid = charge.Id,
                                      Accountid = charge.Accountid,
                                      Appuserid = appuser.Id,
                                      Appusername = appuser.Truename,
                                      Appusercode = appuser.Usercode
                                  }).ToListAsync();

            //代理商成本
            var agentFeeAccountIds = profitResponseList.List.Where(t => t.SubjectCode == "200").Select(t => t.Id).ToList();
            var agents = await (from agentfee in _accountsubjectService.Query<Agentfee>(t => agentFeeAccountIds.Contains(t.Accountid))
                                join agent in _accountsubjectService.Query<Agent>()
                                on agentfee.Agentid equals agent.Id
                                select new
                                {
                                    Agentfeeid = agentfee.Id,
                                    Accountid = agentfee.Accountid,
                                    Agentid = agent.Id,
                                    Agentname = agent.Name
                                }).ToListAsync();

            //运营成本
            var runFeeAccountIds = profitResponseList.List.Where(t => t.SubjectCode == "300").Select(t => t.Id).ToList();
            var runFees = await _accountsubjectService.QueryAsync<Runfee>(t => runFeeAccountIds.Contains(t.Accountid));

            var sysUserids = profitResponseList.List.Select(t => t.Adduser).ToList();
            var sysUsers = await _accountsubjectService.QueryAsync<Sysuser>(t => sysUserids.Contains(t.Id));

            foreach (ProfitResponse profitResponse in profitResponseList.List)
            {
                profitResponse.Currencyname = sumcurrencies.SingleOrDefault(t => t.Id == profitResponse.Currencyid).Name;
                profitResponse.Addusername = sysUsers.SingleOrDefault(t => t.Id == profitResponse.Adduser).Name;
                switch (profitResponse.SubjectCode)
                {
                    case "100":
                        profitResponse.SubjectName = "收入";
                        profitResponse.Description = appusers.SingleOrDefault(t => t.Accountid == profitResponse.Id).Appusercode;
                        break;
                    case "200":
                        profitResponse.SubjectName = "代理商成本";
                        profitResponse.Description = agents.SingleOrDefault(t => t.Accountid == profitResponse.Id).Agentname;
                        break;
                    case "300":
                        profitResponse.SubjectName = "运营成本";
                        profitResponse.Description = runFees.SingleOrDefault(t => t.Accountid == profitResponse.Id).Name;
                        break;
                }
            }

            return RickWebResult.Success(profitResponseList);
        }

        public class ProfitResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public int Direction { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public string SubjectName { get; set; }
            public string SubjectCode { get; set; }
            public string Description { get; set; }

        }
        public class ProfitResponseList
        {
            public int Count { get; set; }
            public List<ProfitResponse> List { get; set; }
            public List<ProfitResponseSum> SumList { get; set; }
            public List<ProfitResponseSum> InList { get; set; }
            public List<ProfitResponseSum> OutList { get; set; }
            public List<ProfitResponseSum> ProfitList { get; set; }
        }
        public class ProfitResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public int Direction { get; set; }
            public decimal TotalAmount { get; set; }
        }

    }
}
