using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApplyController : RickControllerBase
    {
        private readonly ILogger<OrderApplyController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public OrderApplyController(ILogger<OrderApplyController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 申请打包
        /// </summary>
        /// <param name="orderApplyRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] OrderApplyRequest orderApplyRequest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();

            Packageorderapply packageorderapply = new Packageorderapply();
            packageorderapply.Id = _idGenerator.NextId();
            packageorderapply.Status = 1;
            packageorderapply.Adduser = UserInfo.Id;
            packageorderapply.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            packageorderapply.Addtime = now;
            packageorderapply.Lasttime = now;
            packageorderapply.Appuser = UserInfo.Id;
            packageorderapply.Channelid = orderApplyRequest.ChannelId;

            var channelDetail = (await _packageOrderApplyService.QueryAsync<Channeldetail>(cd => cd.Id == orderApplyRequest.ChannelId)).FirstOrDefault();
            packageorderapply.Nationid = channelDetail.Nationid;

            packageorderapply.Addressid = orderApplyRequest.AddressId;
            packageorderapply.Orderstatus = (int)OrderApplyStatus.发起申请;
            packageorderapply.Ispayed = 0;
            packageorderapply.Remark = orderApplyRequest.Remark;
            await _packageOrderApplyService.AddAsync(packageorderapply);

            foreach (long exclaimId in orderApplyRequest.Orders)
            {
                var exclaim = await _packageOrderApplyService.FindAsync<Expressclaim>(exclaimId);
                exclaim.Status = (int)ExpressClaimStatus.已申请;
                await _packageOrderApplyService.UpdateAsync(exclaim);

                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = (long)exclaim.Packageid;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 0;
                packagenote.Operator = (int)PackageNoteStatus.申请发货;
                packagenote.Operatoruser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(packagenote);

                Packageorderapplydetail packageorderapplydetail = new Packageorderapplydetail();
                packageorderapplydetail.Id = _idGenerator.NextId();
                packageorderapplydetail.Status = 1;
                packageorderapplydetail.Adduser = UserInfo.Id;
                packageorderapplydetail.Lastuser = UserInfo.Id;
                packageorderapplydetail.Addtime = now;
                packageorderapplydetail.Lasttime = now;
                packageorderapplydetail.Packageorderapplyid = packageorderapply.Id;
                packageorderapplydetail.Exclaimid = exclaim.Id;
                packageorderapplydetail.Packageid = Convert.ToInt64(exclaim.Packageid);
                await _packageOrderApplyService.AddAsync(packageorderapplydetail);
            }

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 查询已出货录单的打包申请
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderApplyResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from packageorderapply in _packageOrderApplyService.Query<Packageorderapply>(t => t.Status == 1 && t.Orderstatus == (int)OrderApplyStatus.出货录单 && t.Appuser == UserInfo.Id)
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1)
                        on packageorderapply.Id equals packageorderapplyexpress.Packageorderapplyid
                        join nation in _packageOrderApplyService.Query<Nation>(t=> 1 == 1)
                        on packageorderapply.Nationid equals nation.Id
                        select new OrderApplyResponse()
                        {
                            Id = packageorderapply.Id,
                            ExpressId = packageorderapplyexpress.Id,
                            Channelid = packageorderapply.Channelid,
                            Nationid = packageorderapply.Nationid,
                            NationName = nation.Name,
                            Addressid = packageorderapply.Addressid,
                            Addtime = packageorderapply.Addtime,
                            Remark = packageorderapplyexpress.Remark,
                            Mailcode = packageorderapplyexpress.Mailcode,
                            Price = packageorderapplyexpress.Price
                        };

            OrderApplyResponseList orderApplyResponseList = new OrderApplyResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();
            foreach (var item in orderApplyResponseList.List)
            {
                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.FindAsync<Packageorderapplyexpress>(item.ExpressId);
                var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
                item.Details = packageorderapplyexpressdetails.Select(ppp => new OrderApplyResponseDetail() {
                    Count = ppp.Count,
                    Weight = ppp.Weight,
                    Customprice = ppp.Customprice,
                    Sueprice = ppp.Sueprice,
                    Overlengthprice = ppp.Overlengthprice,
                    Overweightprice = ppp.Overweightprice,
                    Oversizeprice = ppp.Oversizeprice,
                    Paperprice = ppp.Paperprice,
                    Boxprice = ppp.Boxprice,
                    Bounceprice = ppp.Bounceprice,
                    Price = ppp.Price
                }).ToList();

            }
            return RickWebResult.Success(orderApplyResponseList);
        }

        /// <summary>
        /// 确认发货
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromQuery] long id)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Appuseraccount appuseraccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == UserInfo.Id && t.Currencyid == 15502590835934208 && t.Status == 1)).FirstOrDefault();
            if (appuseraccount == null)
            {
                appuseraccount = new Appuseraccount();
                appuseraccount.Id = _idGenerator.NextId();
                appuseraccount.Status = 1;
                appuseraccount.Adduser = UserInfo.Id;
                appuseraccount.Lastuser = UserInfo.Id;
                appuseraccount.Addtime = now;
                appuseraccount.Lasttime = now;
                appuseraccount.Appuser = UserInfo.Id;
                appuseraccount.Amount = 0;
                appuseraccount.Currencyid = 15502590835934208;

                await _packageOrderApplyService.AddAsync(appuseraccount);
                await _packageOrderApplyService.CommitAsync();

                return RickWebResult.Error(new object(), 996, "您的余额不足");
            }
            else
            {
                Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(id);
                Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
                packageorderapply.Orderstatus = (int)OrderApplyStatus.确认发货;
                packageorderapply.Paytime = DateTime.Now;
                packageorderapply.Ispayed = 1;
                await _packageOrderApplyService.UpdateAsync(packageorderapply);
                appuseraccount.Amount -= (decimal)packageorderapplyexpress.Price;

                await _packageOrderApplyService.UpdateAsync(appuseraccount);

                Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();

                appuseraccountconsume.Id = _idGenerator.NextId();
                appuseraccountconsume.Status = 1;
                appuseraccountconsume.Adduser = UserInfo.Id;
                appuseraccountconsume.Addtime = now;
                appuseraccountconsume.Appuser = UserInfo.Id;
                appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Price;

                await _packageOrderApplyService.AddAsync(appuseraccountconsume);

                await _packageOrderApplyService.CommitAsync();
                return RickWebResult.Success(new object());
            }
            

        }

        public class OrderApplyRequest
        {
            public IList<long> Orders { get; set; }
            public long ChannelId { get; set; }
            public long AddressId { get; set; }
            public string Remark { get; set; }
        }

        public class OrderApplyResponse
        {
            public long Id { get; set; }
            public long ExpressId { get; set; }
            public long Channelid { get; set; }
            public long Nationid { get; set; }
            public string NationName { get; set; }
            public long Addressid { get; set; }
            public DateTime Addtime { get; set; }
            public string Remark { get; set; }
            public decimal? Price { get; set; }
            public string Mailcode { get; set; }

            public List<OrderApplyResponseDetail> Details { get; set; }
        }

        public class OrderApplyResponseDetail
        {
            public int? Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Customprice { get; set; }
            public decimal? Sueprice { get; set; }
            public decimal? Overlengthprice { get; set; }
            public decimal? Overweightprice { get; set; }
            public decimal? Oversizeprice { get; set; }
            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Price { get; set; }


        }


        public class OrderApplyResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyResponse> List { get; set; }
        }


    }
}
