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
            var exclaims = await _packageOrderApplyService.QueryAsync<Expressclaim>(t => orderApplyRequest.Orders.Contains(t.Id));

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
            var address = await _packageOrderApplyService.FindAsync<Appuseraddress>(orderApplyRequest.AddressId);
            packageorderapply.Addressid = address.Id;
            packageorderapply.Nationid = address.Nationid;
            packageorderapply.Addressid = orderApplyRequest.AddressId;
            packageorderapply.Orderstatus = (int)OrderApplyStatus.申请打包;
            packageorderapply.Ispayed = 0;
            packageorderapply.Remark = orderApplyRequest.Remark;
            //string orderCode = "000000000000" + _redisClientService.OrderCodeGet();
            packageorderapply.Code = _redisClientService.OrderCodeGet();

            await _packageOrderApplyService.AddAsync(packageorderapply);

            Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            packageorderapplynote.Id = _idGenerator.NextId();
            packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            packageorderapplynote.Status = 1;
            packageorderapplynote.Adduser = UserInfo.Id;
            packageorderapplynote.Addtime = now;
            packageorderapplynote.Isclosed = 0;
            packageorderapplynote.Operator = (int)OrderApplyStatus.申请打包;
            packageorderapplynote.Operatoruser = UserInfo.Id;
            await _packageOrderApplyService.AddAsync(packageorderapplynote);

            foreach (long exclaimId in orderApplyRequest.Orders)
            {
                var exclaim = await _packageOrderApplyService.FindAsync<Expressclaim>(exclaimId);
                exclaim.Status = (int)ExpressClaimStatus.申请打包;
                await _packageOrderApplyService.UpdateAsync(exclaim);

                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = (long)exclaim.Packageid;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 0;
                packagenote.Operator = (int)PackageNoteStatus.申请打包;
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
            Appuser appuser = await _packageOrderApplyService.FindAsync<Appuser>(UserInfo.Id);

            Message message = new Message();
            message.Id = _idGenerator.NextId();
            message.Status = 1;
            message.Adduser = appuser.Id;
            message.Lastuser = appuser.Id;
            message.Addtime = now;
            message.Lasttime = now;
            message.Isclosed = 0;
            message.Sender = UserInfo.Id;
            message.Index = "packageApplyOutTable";
            message.Message1 = string.Format("用户:{0}申请打包，内单号:[{1}]", appuser.Usercode, packageorderapply.Code);
            await _packageOrderApplyService.AddAsync(message);

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
            var query = from packageorderapply in _packageOrderApplyService.Query<Packageorderapply>(t => t.Status == 1 && t.Orderstatus == (int)OrderApplyStatus.发货待确认 && t.Appuser == UserInfo.Id)
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1)
                        on packageorderapply.Id equals packageorderapplyexpress.Packageorderapplyid
                        join nation in _packageOrderApplyService.Query<Nation>(t => 1 == 1)
                        on packageorderapply.Nationid equals nation.Id
                        select new OrderApplyResponse()
                        {
                            Id = packageorderapply.Id,
                            Code = packageorderapply.Code,
                            ExpressId = packageorderapplyexpress.Id,
                            Channelid = packageorderapply.Channelid,
                            Nationid = packageorderapply.Nationid,
                            NationName = nation.Name,
                            Addressid = packageorderapply.Addressid,
                            Addtime = packageorderapply.Addtime,
                            Remark = packageorderapplyexpress.Remark,
                            Mailcode = packageorderapplyexpress.Mailcode,
                            Price = packageorderapplyexpress.Price,
                            Freightprice = packageorderapplyexpress.Freightprice
                        };

            OrderApplyResponseList orderApplyResponseList = new OrderApplyResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var expressids = orderApplyResponseList.List.Select(t => t.ExpressId);
            var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => expressids.Contains(t.Packageorderapplyexpressid)).ToListAsync();

            var packageorderapplyids = orderApplyResponseList.List.Select(t => t.Id).ToList();
            //var packages = await _packageOrderApplyService.Query<Packageorderapplydetail>(t => t.Status == 1 && packageorderapplyids.Contains(t.Packageorderapplyid)).ToListAsync();

            var packages = await (from packageorderapplydetail in _packageOrderApplyService.Query<Packageorderapplydetail>(t => t.Status == 1 && packageorderapplyids.Contains(t.Packageorderapplyid))
                                  join package in _packageOrderApplyService.Query<Package>()
                                  on packageorderapplydetail.Packageid equals package.Id
                                  select new
                                  {
                                      PackageorderapplyId = packageorderapplydetail.Packageorderapplyid,
                                      Package = package
                                  }).ToListAsync();

            var packageids = packages.Select(t => t.Package.Id).ToList();
            var imageInfos = await (from image in _packageOrderApplyService.Query<Packageimage>(t => packageids.Contains(t.Packageid))
                                    select image
            ).ToListAsync();

            var vedioInfos = await (from vedio in _packageOrderApplyService.Query<Packagevideo>(t => packageids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            foreach (var item in orderApplyResponseList.List)
            {
                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.FindAsync<Packageorderapplyexpress>(item.ExpressId);
                //var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
                var cpackageorderapplyexpressdetails = packageorderapplyexpressdetails.Where(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id);
                item.Details = cpackageorderapplyexpressdetails.Select(ppp => new OrderApplyResponseDetail()
                {
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
                    Vacuumprice = ppp.Vacuumprice,
                    Remoteprice = ppp.Remoteprice,
                    Packaddprice = ppp.Packaddprice,
                    Haselectrified = ppp.Haselectrified.HasValue && ppp.Haselectrified == 1,
                    Price = ppp.Price,
                }).ToList();

                item.Packages = packages.Where(t => t.PackageorderapplyId == item.Id).Select(t => new OrderApplyResponsePackageDetail()
                {
                    PackageId = t.Package.Id,
                    PackageName = t.Package.Name,
                    Expressnumber = t.Package.Expressnumber,
                    Count = t.Package.Count,
                    Weight = t.Package.Weight,
                    Freightprice = t.Package.Freightprice
                }).ToList();
                foreach (var Detail in item.Packages)
                {
                    Detail.Images = imageInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                    Detail.Videos = vedioInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                }

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
            #region 注释
            //var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            //var localCurrency = currencies.Single(t => t.Islocal == 1);//人民币
            //var defaultCurrency = currencies.Single(t => t.Isdefault == 1);//美元

            //Appuseraccount appuserlocalaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == UserInfo.Id && t.Currencyid == localCurrency.Id && t.Status == 1)).FirstOrDefault();
            //Appuseraccount appuserdefaultaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == UserInfo.Id && t.Currencyid == defaultCurrency.Id && t.Status == 1)).FirstOrDefault();
            //if (appuserlocalaccount == null || appuserdefaultaccount == null)
            //{
            //    if (appuserlocalaccount == null)
            //    {
            //        appuserlocalaccount = new Appuseraccount();
            //        appuserlocalaccount.Id = _idGenerator.NextId();
            //        appuserlocalaccount.Status = 1;
            //        appuserlocalaccount.Adduser = UserInfo.Id;
            //        appuserlocalaccount.Lastuser = UserInfo.Id;
            //        appuserlocalaccount.Addtime = now;
            //        appuserlocalaccount.Lasttime = now;
            //        appuserlocalaccount.Appuser = UserInfo.Id;
            //        appuserlocalaccount.Amount = 0;
            //        appuserlocalaccount.Currencyid = localCurrency.Id;

            //        await _packageOrderApplyService.AddAsync(appuserlocalaccount);
            //    }

            //    if (appuserdefaultaccount == null)
            //    {
            //        appuserdefaultaccount = new Appuseraccount();
            //        appuserdefaultaccount.Id = _idGenerator.NextId();
            //        appuserdefaultaccount.Status = 1;
            //        appuserdefaultaccount.Adduser = UserInfo.Id;
            //        appuserdefaultaccount.Lastuser = UserInfo.Id;
            //        appuserdefaultaccount.Addtime = now;
            //        appuserdefaultaccount.Lasttime = now;
            //        appuserdefaultaccount.Appuser = UserInfo.Id;
            //        appuserdefaultaccount.Amount = 0;
            //        appuserdefaultaccount.Currencyid = defaultCurrency.Id;

            //        await _packageOrderApplyService.AddAsync(appuserdefaultaccount);
            //    }
            //    await _packageOrderApplyService.CommitAsync();

            //    return RickWebResult.Error(new object(), 996, "您的余额不足");
            //}
            //else
            //{
            //    Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(id);
            //    if (packageorderapply.Status != (int)OrderApplyStatus.发货待确认)
            //    {
            //        RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            //    }

            //    Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
            //    packageorderapply.Orderstatus = (int)OrderApplyStatus.待发货;
            //    packageorderapply.Paytime = DateTime.Now;
            //    packageorderapply.Ispayed = 1;
            //    await _packageOrderApplyService.UpdateAsync(packageorderapply);

            //    appuserdefaultaccount.Amount -= (decimal)packageorderapplyexpress.Price;

            //    await _packageOrderApplyService.UpdateAsync(appuserdefaultaccount);

            //    Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();

            //    appuseraccountconsume.Id = _idGenerator.NextId();
            //    appuseraccountconsume.Status = 1;
            //    appuseraccountconsume.Adduser = UserInfo.Id;
            //    appuseraccountconsume.Addtime = now;
            //    appuseraccountconsume.Appuser = UserInfo.Id;
            //    appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Price;

            //    await _packageOrderApplyService.AddAsync(appuseraccountconsume);

            //    Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            //    packageorderapplynote.Id = _idGenerator.NextId();
            //    packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            //    packageorderapplynote.Status = 1;
            //    packageorderapplynote.Adduser = UserInfo.Id;
            //    packageorderapplynote.Addtime = now;
            //    packageorderapplynote.Isclosed = 0;
            //    packageorderapplynote.Operator = (int)OrderApplyStatus.待发货;
            //    packageorderapplynote.Operatoruser = UserInfo.Id;
            //    await _packageOrderApplyService.AddAsync(packageorderapplynote);

            //    var packageorderapplydetailes = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id);
            //    foreach (var packageorderapplydetaile in packageorderapplydetailes)
            //    {
            //        Packagenote packagenote = new Packagenote();
            //        packagenote.Id = _idGenerator.NextId();
            //        packagenote.Packageid = packageorderapplydetaile.Packageid;
            //        packagenote.Status = 1;
            //        packagenote.Adduser = UserInfo.Id;
            //        packagenote.Addtime = now;
            //        packagenote.Isclosed = 0;
            //        packagenote.Operator = (int)PackageNoteStatus.待发货;
            //        packagenote.Operatoruser = UserInfo.Id;
            //        await _packageOrderApplyService.AddAsync(packagenote);
            //    }

            //    await _packageOrderApplyService.CommitAsync();
            //    return RickWebResult.Success(new object());
            //}
            #endregion
            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(id);
            if (packageorderapply.Status != (int)OrderApplyStatus.发货待确认)
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }

            Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
            packageorderapply.Orderstatus = (int)OrderApplyStatus.待发货;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);

            Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            packageorderapplynote.Id = _idGenerator.NextId();
            packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            packageorderapplynote.Status = 1;
            packageorderapplynote.Adduser = UserInfo.Id;
            packageorderapplynote.Addtime = now;
            packageorderapplynote.Isclosed = 0;
            packageorderapplynote.Operator = (int)OrderApplyStatus.待发货;
            packageorderapplynote.Operatoruser = UserInfo.Id;
            await _packageOrderApplyService.AddAsync(packageorderapplynote);

            var packageorderapplydetailes = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id);
            foreach (var packageorderapplydetaile in packageorderapplydetailes)
            {
                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = packageorderapplydetaile.Packageid;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 0;
                packagenote.Operator = (int)PackageNoteStatus.待发货;
                packagenote.Operatoruser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(packagenote);
            }
            Appuser appuser = await _packageOrderApplyService.FindAsync<Appuser>(UserInfo.Id);

            Message message = new Message();
            message.Id = _idGenerator.NextId();
            message.Status = 1;
            message.Adduser = appuser.Id;
            message.Lastuser = appuser.Id;
            message.Addtime = now;
            message.Lasttime = now;
            message.Isclosed = 0;
            message.Sender = UserInfo.Id;
            message.Index = "packageDeliverTable";
            message.Message1 = string.Format("用户:{0}确认发货，内单号:[{1}]", appuser.Usercode, packageorderapply.Code);
            await _packageOrderApplyService.AddAsync(message);

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());
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
            public string Code { get; set; }

            public long ExpressId { get; set; }
            public long Channelid { get; set; }
            public long Nationid { get; set; }
            public string NationName { get; set; }
            public long Addressid { get; set; }
            public DateTime Addtime { get; set; }
            public string Remark { get; set; }
            public decimal? Price { get; set; }
            public decimal? Freightprice { get; set; }
            public string Mailcode { get; set; }
            public List<OrderApplyResponsePackageDetail> Packages { get; set; }
            public List<OrderApplyResponseDetail> Details { get; set; }
        }
        public class OrderApplyResponsePackageDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Freightprice { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
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
            public decimal? Vacuumprice { get; set; }
            public decimal? Remoteprice { get; set; }
            public bool Haselectrified { get; set; }
            public decimal? Packaddprice { get; set; }
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
