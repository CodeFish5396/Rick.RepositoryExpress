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
        /// 查询用户的申请打包
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="packageUserName"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderApplyResponseList>> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string packageUserName, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        && (t.Orderstatus == (int)OrderApplyStatus.申请打包 || t.Orderstatus == (int)OrderApplyStatus.发货待确认)
                        )
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t =>
                            (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Packuser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(packageUserName) || (sysUser != null && sysUser.Name == packageUserName))
                        select new OrderApplyResponse()
                        {
                            Code = order.Code,
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Channelprice = channel.Unitprice,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            Appuser = order.Appuser,
                            AppuserCode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed,
                            Paytime = order.Paytime,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Packagetime = order.Packtime,
                            Packageuser = sysUser == null ? 0 : sysUser.Id,
                            Packageusername = sysUser == null ? string.Empty : sysUser.Name,
                        };

            OrderApplyResponseList orderApplyResponseList = new OrderApplyResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Id).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var chaneelIds = orderApplyResponseList.List.Select(t => t.Channelid).ToList();
            var channelPrices = await _packageOrderApplyService.QueryAsync<Channelprice>(t => t.Status == 1 && chaneelIds.Contains(t.Channelid));
            foreach (OrderApplyResponse orderApplyResponse in orderApplyResponseList.List)
            {
                orderApplyResponse.Pricedetails = channelPrices.Where(t => t.Channelid == orderApplyResponse.Channelid && t.Nationid == orderApplyResponse.Nationid).Select(t => new ChannelResponsepricedetail()
                {
                    Id = t.Id,
                    Channelid = t.Channelid,
                    Nationid = t.Nationid,
                    Minweight = t.Minweight,
                    Maxweight = t.Maxweight,
                    Price = t.Price
                }).ToList();
            }

            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);
            Currencychangerate currentRate = await _packageOrderApplyService.Query<Currencychangerate>(t => t.Status == 1 && t.Sourcecurrency == defaultCurrency.Id && t.Targetcurrency == localCurrency.Id).SingleAsync();
            orderApplyResponseList.Currencychangerateid = currentRate.Id;
            orderApplyResponseList.Currencychangerate = currentRate.Rate;

            foreach (var item in orderApplyResponseList.List)
            {
                var orderid = item.Id;
                var packageIds = await (from package in _packageOrderApplyService.Query<Packageorderapplydetail>(t => orderid == t.Packageorderapplyid && t.Status == 1)
                                        select package.Packageid
                        ).ToListAsync();

                var packages = await (from package in _packageOrderApplyService.Query<Package>(t => packageIds.Contains(t.Id))
                                      select package
                            ).ToListAsync();
                item.Details = packages.Select(package => new OrderApplyResponseDetail()
                {
                    PackageId = package.Id,
                    PackageCode = package.Code,
                    PackageName = package.Name,
                    Expressnumber = package.Expressnumber,
                    Location = package.Location,
                    Name = package.Name,
                    Count = package.Count,
                    Weight = package.Weight,
                    Payedprice = package.Freightprice
                }).ToList();
                var imageInfos = await (from image in _packageOrderApplyService.Query<Packageimage>(t => packageIds.Contains(t.Packageid))
                                        select image
                            ).ToListAsync();

                var vedioInfos = await (from vedio in _packageOrderApplyService.Query<Packagevideo>(t => packageIds.Contains(t.Packageid))
                                        select vedio
                            ).ToListAsync();
                foreach (var Detail in item.Details)
                {
                    Detail.Images = imageInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                    Detail.Videos = vedioInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                }

                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Packageorderapplyid == orderid && t.Status == 1).SingleOrDefaultAsync();
                item.PostedDetails = new OrderApplyPostResponse();
                if (packageorderapplyexpress != null)
                {
                    item.PostedDetails.Remark = packageorderapplyexpress.Remark;
                    item.PostedDetails.Price = packageorderapplyexpress.Price;
                    item.PostedDetails.Mailcode = packageorderapplyexpress.Mailcode;
                    var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
                    item.PostedDetails.Details = packageorderapplyexpressdetails.Select(t => new OrderApplyPostResponsedetail()
                    {
                        Id = t.Id,
                        Count = t.Count,
                        Weight = t.Weight,
                        Customprice = t.Customprice,
                        Sueprice = t.Sueprice,
                        Overlengthprice = t.Overlengthprice,
                        Overweightprice = t.Overweightprice,
                        Oversizeprice = t.Oversizeprice,
                        Paperprice = t.Paperprice,
                        Boxprice = t.Boxprice,
                        Bounceprice = t.Bounceprice,
                        Vacuumprice = t.Vacuumprice,
                        PackAddPrice = t.Packaddprice,
                        HasPackAddPrice = t.Packaddprice.HasValue && t.Packaddprice !=0,
                        RemotePrice = t.Remoteprice,
                        HasRemote = t.Remoteprice.HasValue && t.Remoteprice != 0,
                        HasElectrified = t.Haselectrified.HasValue && t.Haselectrified != 0,
                        Price = t.Price,
                        Length = t.Length,
                        Width = t.Width,
                        Height = t.Height,
                        Volumeweight = t.Volumeweight
                    }).ToList();
                    foreach (var orderapplypostresponsedetail in item.PostedDetails.Details)
                    {
                        var packageexpress = await _packageOrderApplyService.Query<Packageorderapplyexpresspackage>(t => t.Packageorderapplyexpressdetailsid == orderapplypostresponsedetail.Id).ToListAsync();
                        var packageids = packageexpress.Select(t => t.Packageid);
                        orderapplypostresponsedetail.Packages = packages.Where(t => packageids.Contains(t.Id)).Select(package => new OrderApplyPostResponseDetail()
                        {
                            PackageId = package.Id,
                            PackageName = package.Name,
                        }).ToList();
                    }
                }
            }

            return RickWebResult.Success(orderApplyResponseList);
        }

        /// <summary>
        /// 出货录单
        /// </summary>
        /// <param name="orderApplyRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] OrderApplyRequest orderApplyRequest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyRequest.Id);
            if (packageorderapply.Status != (int)OrderApplyStatus.申请打包)
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }

            packageorderapply.Orderstatus = (int)OrderApplyStatus.发货待确认;
            packageorderapply.Lasttime = now;
            packageorderapply.Lastuser = UserInfo.Id;
            packageorderapply.Packtime = packageorderapply.Lasttime;
            packageorderapply.Packuser = UserInfo.Id;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);
            decimal freightPrice = 0m;
            var packageorderapplydetailes = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id);
            foreach (var packageorderapplydetaile in packageorderapplydetailes)
            {
                var package = await _packageOrderApplyService.FindAsync<Package>(packageorderapplydetaile.Packageid);

                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = package.Id;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 0;
                packagenote.Operator = (int)PackageNoteStatus.发货待确认;
                packagenote.Operatoruser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(packagenote);

                freightPrice += package.Freightprice ?? 0;
            }


            var oldexpresses = await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Status == 1 && t.Packageorderapplyid == packageorderapply.Id);
            foreach (var old in oldexpresses)
            {
                old.Status = 0;
                await _packageOrderApplyService.UpdateAsync(old);
            }

            Currencychangerate currentRate = await _packageOrderApplyService.FindAsync<Currencychangerate>(orderApplyRequest.Currencychangerateid);

            Packageorderapplyexpress packageorderapplyexpress = new Packageorderapplyexpress();
            packageorderapplyexpress.Id = _idGenerator.NextId();
            packageorderapplyexpress.Packageorderapplyid = packageorderapply.Id;
            packageorderapplyexpress.Remark = orderApplyRequest.Remark;
            packageorderapplyexpress.Mailcode = orderApplyRequest.Mailcode;
            packageorderapplyexpress.Price = orderApplyRequest.Price;
            packageorderapplyexpress.Status = 1;
            packageorderapplyexpress.Addtime = now;
            packageorderapplyexpress.Lasttime = now;
            packageorderapplyexpress.Adduser = UserInfo.Id;
            packageorderapplyexpress.Lastuser = UserInfo.Id;
            packageorderapplyexpress.Currencychangerateid = currentRate.Id;
            packageorderapplyexpress.Currencychangerate = currentRate.Rate;
            packageorderapplyexpress.Targetprice = orderApplyRequest.Targetprice;
            packageorderapplyexpress.Freightprice = freightPrice;
            await _packageOrderApplyService.AddAsync(packageorderapplyexpress);

            foreach (OrderApplyRequestdetail orderApplyRequestdetail in orderApplyRequest.Details)
            {
                Packageorderapplyexpressdetail packageorderapplyexpressdetail = new Packageorderapplyexpressdetail();
                packageorderapplyexpressdetail.Id = _idGenerator.NextId();
                packageorderapplyexpressdetail.Packageorderapplyexpressid = packageorderapplyexpress.Id;
                packageorderapplyexpressdetail.Length = orderApplyRequestdetail.Length;
                packageorderapplyexpressdetail.Width = orderApplyRequestdetail.Width;
                packageorderapplyexpressdetail.Height = orderApplyRequestdetail.Height;
                packageorderapplyexpressdetail.Weight = orderApplyRequestdetail.Weight;
                packageorderapplyexpressdetail.Volumeweight = orderApplyRequestdetail.Volumeweight;
                packageorderapplyexpressdetail.Count = orderApplyRequestdetail.Count;
                packageorderapplyexpressdetail.Customprice = orderApplyRequestdetail.Customprice;
                packageorderapplyexpressdetail.Sueprice = orderApplyRequestdetail.Sueprice;
                packageorderapplyexpressdetail.Overlengthprice = orderApplyRequestdetail.Overlengthprice;
                packageorderapplyexpressdetail.Overweightprice = orderApplyRequestdetail.Overweightprice;
                packageorderapplyexpressdetail.Oversizeprice = orderApplyRequestdetail.Oversizeprice;
                packageorderapplyexpressdetail.Vacuumprice = orderApplyRequestdetail.Vacuumprice;
                packageorderapplyexpressdetail.Remoteprice = orderApplyRequestdetail.RemotePrice;
                packageorderapplyexpressdetail.Haselectrified = (sbyte)((orderApplyRequestdetail.Haselectrified.HasValue && orderApplyRequestdetail.Haselectrified == true) ? 1 : 0);
                packageorderapplyexpressdetail.Packaddprice = orderApplyRequestdetail.PackAddPrice;

                packageorderapplyexpressdetail.Paperprice = orderApplyRequestdetail.Paperprice;
                packageorderapplyexpressdetail.Boxprice = orderApplyRequestdetail.Boxprice;
                packageorderapplyexpressdetail.Bounceprice = orderApplyRequestdetail.Bounceprice;
                packageorderapplyexpressdetail.Price = orderApplyRequestdetail.Price;
                packageorderapplyexpressdetail.Currencychangerateid = currentRate.Id;
                packageorderapplyexpressdetail.Currencychangerate = currentRate.Rate;
                packageorderapplyexpressdetail.Targetprice = orderApplyRequestdetail.Targetprice;

                foreach (long packageid in orderApplyRequestdetail.Packages)
                {
                    Packageorderapplyexpresspackage packageorderapplyexpresspackage = new Packageorderapplyexpresspackage();
                    packageorderapplyexpresspackage.Id = _idGenerator.NextId();
                    packageorderapplyexpresspackage.Packageorderapplyexpressdetailsid = packageorderapplyexpressdetail.Id;
                    packageorderapplyexpresspackage.Packageid = packageid;
                    await _packageOrderApplyService.AddAsync(packageorderapplyexpresspackage);
                }
                await _packageOrderApplyService.AddAsync(packageorderapplyexpressdetail);
            }

            Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            packageorderapplynote.Id = _idGenerator.NextId();
            packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            packageorderapplynote.Status = 1;
            packageorderapplynote.Adduser = UserInfo.Id;
            packageorderapplynote.Addtime = now;
            packageorderapplynote.Isclosed = 0;
            packageorderapplynote.Operator = (int)OrderApplyStatus.发货待确认;
            packageorderapplynote.Operatoruser = UserInfo.Id;
            await _packageOrderApplyService.AddAsync(packageorderapplynote);


            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class OrderApplyRequest
        {
            public long Id { get; set; }
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public long Currencychangerateid { get; set; }
            public decimal? Targetprice { get; set; }
            public IList<OrderApplyRequestdetail> Details { get; set; }
        }
        public class OrderApplyRequestdetail
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
            public decimal? Vacuumprice { get; set; }
            public decimal? Price { get; set; }
            public decimal? Targetprice { get; set; }
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Volumeweight { get; set; }
            public decimal? RemotePrice { get; set; }
            public bool? Haselectrified { get; set; }
            public decimal? PackAddPrice { get; set; }

            public List<long> Packages { get; set; }
        }
        public class OrderApplyResponse
        {
            public string Code { get; set; }
            public long Id { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public decimal Channelprice { get; set; }
            public List<ChannelResponsepricedetail> Pricedetails { get; set; }

            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public long Appuser { get; set; }
            public string AppuserCode { get; set; }
            public string Appusername { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public int Status { get; set; }
            public long Packageuser { get; set; }
            public string Packageusername { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime? Packagetime { get; set; }

            public List<OrderApplyResponseDetail> Details { get; set; }
            public OrderApplyPostResponse PostedDetails { get; set; }
        }
        public class OrderApplyPostResponse
        {
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public IList<OrderApplyPostResponsedetail> Details { get; set; }
        }
        public class OrderApplyPostResponsedetail
        {
            public long Id { get; set; }
            public int? Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Customprice { get; set; }
            public decimal? Sueprice { get; set; }
            public decimal? Overlengthprice { get; set; }
            public decimal? Overweightprice { get; set; }
            public decimal? Oversizeprice { get; set; }
            public decimal? Vacuumprice { get; set; }
            public decimal? RemotePrice { get; set; }
            public bool HasElectrified { get; set; }
            public bool HasPackAddPrice { get; set; }
            public bool HasRemote { get; set; }
            public decimal? PackAddPrice { get; set; }
            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Price { get; set; }
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Volumeweight { get; set; }
            public List<OrderApplyPostResponseDetail> Packages { get; set; }
        }
        public class OrderApplyPostResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
        }

        public class OrderApplyResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageCode { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Payedprice { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }

        public class OrderApplyResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyResponse> List { get; set; }
            public long? Currencychangerateid { get; set; }
            public decimal? Currencychangerate { get; set; }

        }

        public class ChannelResponsepricedetail
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public long Nationid { get; set; }
            public decimal Minweight { get; set; }
            public decimal Maxweight { get; set; }
            public decimal Price { get; set; }
        }

    }
}
