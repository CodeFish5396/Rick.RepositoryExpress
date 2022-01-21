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
    /// 订单核算
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OrderProfitController : RickControllerBase
    {
        private readonly ILogger<OrderProfitController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        public OrderProfitController(ILogger<OrderProfitController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询订单核算
        /// </summary>
        /// <param name="agentid"></param>
        /// <param name="nationid"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderProfitResponseList>> Get([FromQuery] long? agentid, [FromQuery] long? nationid, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            OrderProfitResponseList profitResponseList = new OrderProfitResponseList();

            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t =>
                        (t.Orderstatus == (int)OrderApplyStatus.已发货 || t.Orderstatus == (int)OrderApplyStatus.已签收)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1)
                        on order.Id equals packageorderapplyexpress.Packageorderapplyid
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => !nationid.HasValue || t.Id == nationid)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>()
                        on order.Appuser equals appuser.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Senduser equals sysUser.Id
                        join courier in _packageOrderApplyService.Query<Courier>()
                        on packageorderapplyexpress.Courierid equals courier.Id
                        join agent in _packageOrderApplyService.Query<Agent>(t => !agentid.HasValue || t.Id == agentid)
                        on packageorderapplyexpress.Agentid equals agent.Id
                        join appuseraccountconsume in _packageOrderApplyService.Query<Appuseraccountconsume>(t => t.Status == 1)
                        on order.Id equals appuseraccountconsume.Orderid
                        join agentfeeconsume in _packageOrderApplyService.Query<Agentfeeconsume>(t => t.Status == 1)
                        on order.Id equals agentfeeconsume.Orderid
                        select new OrderProfitResponse()
                        {
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Appuser = order.Appuser,
                            Appusercode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Paytime = order.Paytime,
                            Price = packageorderapplyexpress.Price ?? 0,
                            Outnumber = packageorderapplyexpress.Outnumber,
                            Innernumber = packageorderapplyexpress.Innernumber,
                            Agentid = packageorderapplyexpress.Agentid,
                            Agentname = agent.Name,
                            Agentprice = packageorderapplyexpress.Agentprice ?? 0,
                            Freightprice = packageorderapplyexpress.Freightprice ?? 0,
                            Courierid = packageorderapplyexpress.Courierid,
                            Couriername = courier.Name,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Senduser = sysUser.Id,
                            Sendusername = sysUser.Name,
                            Sendtime = order.Sendtime,
                            Usercurrencyid = appuseraccountconsume.Curencyid ?? 0,
                            Useramount = appuseraccountconsume.Amount,
                            Agentcurrencyid = agentfeeconsume.Currencyid,
                            Agentamount = agentfeeconsume.Amount
                        };

            var sumIncome = await (from q in query
                                   select new
                                   {
                                       Currencyid = q.Usercurrencyid,
                                       Amount = q.Useramount
                                   }
                            into qt
                                   group qt.Amount by qt.Currencyid
                            into qGT
                                   select new OrderProfitDetail()
                                   {
                                       Currencyid = qGT.Key,
                                       Amount = qGT.Sum()
                                   }).ToListAsync();

            var sumOutcome = await (from q in query
                                    select new
                                    {
                                        Currencyid = q.Agentcurrencyid,
                                        Amount = q.Agentamount
                                    }
                            into qt
                                    group qt.Amount by qt.Currencyid
                            into qGT
                                    select new OrderProfitDetail()
                                    {
                                        Currencyid = qGT.Key,
                                        Amount = qGT.Sum()
                                    }).ToListAsync();

            var SumProfit = (from pro in (from income in sumIncome
                                          select new
                                          {
                                              Currencyid = income.Currencyid,
                                              Amount = income.Amount,
                                              Direction = 1
                                          }
                             ).Union(from income in sumOutcome
                                     select new
                                     {
                                         Currencyid = income.Currencyid,
                                         Amount = income.Amount,
                                         Direction = -1
                                     })
                             group new { pro.Amount, pro.Direction } by pro.Currencyid
                             into proGT
                             select new OrderProfitDetail()
                             {
                                 Currencyid = proGT.Key,
                                 Amount = proGT.Sum(t => t.Amount * t.Direction)
                             }).ToList();

            profitResponseList.Count = await query.CountAsync();
            profitResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            var syscurrencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = syscurrencies.Single(t => t.Islocal == 1);
            var defaultCurrency = syscurrencies.Single(t => t.Isdefault == 1);

            foreach (var profit in profitResponseList.List)
            {
                var pp = (Enumerable.Repeat(new
                {
                    Currencyid = profit.Agentcurrencyid,
                    Amount = profit.Agentamount,
                    Direction = -1
                }, 1)).Union(Enumerable.Repeat(new
                {
                    Currencyid = profit.Usercurrencyid,
                    Amount = profit.Useramount,
                    Direction = 1
                }, 1));

                if (profit.Freightprice > 0)
                {
                    pp = pp.Union(Enumerable.Repeat(new
                    {
                        Currencyid = defaultCurrency.Id,
                        Amount = profit.Freightprice,
                        Direction = -1
                    }, 1));
                }
                profit.Profit = (from p in pp
                                 group new { p.Amount, p.Direction } by p.Currencyid
                                 into proGT
                                 select new OrderProfitDetail()
                                 {
                                     Currencyid = proGT.Key,
                                     Amount = proGT.Sum(t => t.Amount * t.Direction)
                                 }).ToList();
            }

            profitResponseList.SumIncome = sumIncome;
            profitResponseList.SumOutcome = sumOutcome;
            profitResponseList.SumProfit = SumProfit;

            var currencyIds = profitResponseList.List.Select(t => t.Usercurrencyid).ToList();
            currencyIds.AddRange(profitResponseList.List.Select(t => t.Agentcurrencyid));
            currencyIds.AddRange(syscurrencies.Select(t => t.Id));
            currencyIds.AddRange(sumIncome.Select(t => t.Currencyid));
            currencyIds.AddRange(sumOutcome.Select(t => t.Currencyid));

            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(c => currencyIds.Contains(c.Id));

            foreach (OrderProfitResponse appUserDataResponse in profitResponseList.List)
            {
                appUserDataResponse.Usercurrencyname = currencies.Single(t => t.Id == appUserDataResponse.Usercurrencyid).Name;
                appUserDataResponse.Agentcurrencyname = currencies.Single(t => t.Id == appUserDataResponse.Agentcurrencyid).Name;

                foreach (OrderProfitDetail orderProfitDetail in appUserDataResponse.Profit)
                {
                    orderProfitDetail.Currencyname = currencies.Single(t => t.Id == orderProfitDetail.Currencyid).Name;
                }
            }

            foreach (OrderProfitDetail orderProfitDetail in profitResponseList.SumIncome)
            {
                orderProfitDetail.Currencyname = currencies.Single(t => t.Id == orderProfitDetail.Currencyid).Name;
            }

            foreach (OrderProfitDetail orderProfitDetail in profitResponseList.SumOutcome)
            {
                orderProfitDetail.Currencyname = currencies.Single(t => t.Id == orderProfitDetail.Currencyid).Name;
            }

            foreach (OrderProfitDetail orderProfitDetail in profitResponseList.SumProfit)
            {
                orderProfitDetail.Currencyname = currencies.Single(t => t.Id == orderProfitDetail.Currencyid).Name;
            }

            return RickWebResult.Success(profitResponseList);
        }

        public class OrderProfitResponse
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Appuser { get; set; }
            public string Appusername { get; set; }
            public string Appusercode { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public DateTime? Paytime { get; set; }
            public decimal Price { get; set; }
            public decimal Freightprice { get; set; }

            public string Outnumber { get; set; }
            public string Innernumber { get; set; }
            public long? Agentid { get; set; }
            public string Agentname { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public decimal Agentprice { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }

            public long Usercurrencyid { get; set; }
            public string Usercurrencyname { get; set; }
            public decimal Useramount { get; set; }

            public long Agentcurrencyid { get; set; }
            public string Agentcurrencyname { get; set; }
            public decimal Agentamount { get; set; }
            public List<OrderProfitDetail> Profit { get; set; }
        }

        public class OrderProfitResponseList
        {
            public List<OrderProfitDetail> SumIncome { get; set; }
            public List<OrderProfitDetail> SumOutcome { get; set; }
            public List<OrderProfitDetail> SumProfit { get; set; }
            public int Count { get; set; }
            public List<OrderProfitResponse> List { get; set; }
        }
        public class OrderProfitDetail
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }

        }

    }
}
