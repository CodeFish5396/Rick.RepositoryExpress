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
                            Sendtime = order.Sendtime
                        };
            var sumIncome = await query.SumAsync(profit => profit.Price - profit.Freightprice);
            var sumOutcome = await query.SumAsync(profit => profit.Agentprice);

            profitResponseList.Count = await query.CountAsync();
            profitResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            foreach (var profit in profitResponseList.List)
            {
                profit.Price = profit.Price - profit.Freightprice;
                profit.Profit = profit.Price - profit.Agentprice - profit.Freightprice;
            }
            profitResponseList.SumIncome = sumIncome;
            profitResponseList.SumOutcome = sumOutcome;
            profitResponseList.SumProfit = profitResponseList.SumIncome - profitResponseList.SumOutcome;
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
            public decimal Profit { get; set; }

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

        }

        public class OrderProfitResponseList
        {
            public decimal SumIncome { get; set; }
            public decimal SumOutcome { get; set; }
            public decimal SumProfit { get; set; }
            public int Count { get; set; }
            public List<OrderProfitResponse> List { get; set; }
        }

    }
}
