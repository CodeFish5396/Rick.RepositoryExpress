﻿using Microsoft.AspNetCore.Http;
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
    public class OrderApplyExpressController : RickControllerBase
    {
        private readonly ILogger<OrderApplyExpressController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public OrderApplyExpressController(ILogger<OrderApplyExpressController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户确认发货
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="expressNumber"></param>
        /// <param name="userId"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderApplyExpressResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int orderStatus, [FromQuery] string expressNumber, [FromQuery] long? userId, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status) && t.Orderstatus == (int)OrderApplyStatus.确认发货)
                        join channeldetail in _packageOrderApplyService.Query<Channeldetail>(channel => 1 == 1)
                        on order.Channelid equals channeldetail.Id
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on channeldetail.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on channeldetail.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => true)
                        on order.Appuser equals appuser.Id
                        select new OrderApplyExpressResponse()
                        {
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            Appuser = order.Appuser,
                            Appusername = appuser.Name,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed,
                            Paytime = order.Paytime,
                            Status = order.Status,
                            Addtime = order.Addtime
                        };

            OrderApplyExpressResponseList orderApplyExpressList = new OrderApplyExpressResponseList();
            orderApplyExpressList.Count = await query.CountAsync();
            orderApplyExpressList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();
            return RickWebResult.Success(orderApplyExpressList);

        }

        /// <summary>
        /// 发货
        /// </summary>
        /// <param name="orderApplyExpressResquest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] OrderApplyExpressResquest orderApplyExpressResquest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyExpressResquest.Id);
            packageorderapply.Orderstatus = (int)OrderApplyStatus.已发货;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);

            var details = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t=>t.Packageorderapplyid == packageorderapply.Id);
            foreach (var detail in details)
            {
                Expressclaim expressclaim = await _packageOrderApplyService.FindAsync<Expressclaim>(detail.Exclaimid);
                expressclaim.Status = (int)ExpressClaimStatus.已发货;
                expressclaim.Lasttime = now;
                expressclaim.Lastuser = UserInfo.Id;
                await _packageOrderApplyService.UpdateAsync(expressclaim);

                Package package = await _packageOrderApplyService.FindAsync<Package>(detail.Packageid);
                package.Status = (int)PackageStatus.已出库;
                package.Lasttime = now;
                package.Lastuser = UserInfo.Id;
                await _packageOrderApplyService.UpdateAsync(package);

                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = package.Id;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 0;
                packagenote.Operator = (int)PackageNoteStatus.已发货;
                packagenote.Operatoruser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(packagenote);
            }

            Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
            packageorderapplyexpress.Innernumber = orderApplyExpressResquest.Innernumber;
            packageorderapplyexpress.Batchnumber = orderApplyExpressResquest.Batchnumber;
            packageorderapplyexpress.Agentprice = orderApplyExpressResquest.Agentprice;
            await _packageOrderApplyService.UpdateAsync(packageorderapplyexpress);

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        public class OrderApplyExpressResquest
        {
            public long Id { get; set; }
            public string Innernumber { get; set; }
            public string Batchnumber { get; set; }
            public decimal? Agentprice { get; set; }

        }
        public class OrderApplyExpressResponse
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public long Appuser { get; set; }
            public string Appusername { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class OrderApplyExpressResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyExpressResponse> List { get; set; }

        }
    }
}
