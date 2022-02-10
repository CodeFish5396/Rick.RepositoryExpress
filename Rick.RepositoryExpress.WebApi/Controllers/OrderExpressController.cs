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
    public class OrderExpressController : RickControllerBase
    {
        private readonly ILogger<OrderExpressController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageorderapplyexpressService _packageorderapplyexpressService;
        private readonly RedisClientService _redisClientService;

        public OrderExpressController(ILogger<OrderExpressController> logger, IPackageorderapplyexpressService packageorderapplyexpressService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageorderapplyexpressService = packageorderapplyexpressService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderExpressResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from packageorderapply in _packageorderapplyexpressService.Query<Packageorderapply>(t => t.Status == 1 && t.Orderstatus >= (int)OrderApplyStatus.申请打包 && t.Orderstatus != (int)OrderApplyStatus.问题件 
                        && t.Appuser == UserInfo.Id
                        )
                        join address in _packageorderapplyexpressService.Query<Appuseraddress>(t => 1 == 1)
                        on packageorderapply.Addressid equals address.Id
                        join nation in _packageorderapplyexpressService.Query<Nation>(t => 1 == 1)
                        on address.Nationid equals nation.Id
                        join channel in _packageorderapplyexpressService.Query<Channel>(t => 1 == 1)
                        on packageorderapply.Channelid equals channel.Id
                        select new OrderExpressResponse()
                        {
                            Id = packageorderapply.Id,
                            Code = packageorderapply.Code,
                            Channelid = packageorderapply.Channelid,
                            Channelname = channel.Name,
                            Nationid = packageorderapply.Nationid,
                            NationName = nation.Name,
                            Addressid = packageorderapply.Addressid,
                            Name = address.Name,
                            Contactnumber = address.Contactnumber,
                            Region = address.Region,
                            Address = address.Address,
                            Addtime = packageorderapply.Addtime,
                            Orderstatus = packageorderapply.Orderstatus,
                        };

            OrderExpressResponseList orderApplyResponseList = new OrderExpressResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var appliesIds = orderApplyResponseList.List.Select(t => t.Id);

            var applynotes = await _packageorderapplyexpressService.QueryAsync<Packageorderapplynote>(t => t.Status == 1 && appliesIds.Contains(t.Packageorderapplyid));
            var packageorderapplyexpresses = await _packageorderapplyexpressService.QueryAsync<Packageorderapplyexpress>(t => t.Status == 1 && appliesIds.Contains(t.Packageorderapplyid));

            var packageorderapplyexpressids = packageorderapplyexpresses.Select(t => t.Id);
            var packageorderapplyexpressdetails = await _packageorderapplyexpressService.QueryAsync<Packageorderapplyexpressdetail>(t => packageorderapplyexpressids.Contains(t.Packageorderapplyexpressid));

            var agentids = packageorderapplyexpresses.Select(t => t.Agentid);
            var agents = await _packageorderapplyexpressService.QueryAsync<Agent>(t => agentids.Contains(t.Id));

            var courierIds = packageorderapplyexpresses.Select(t => t.Courierid);
            var couriers = await _packageorderapplyexpressService.QueryAsync<Courier>(t => courierIds.Contains(t.Id));

            var packages = await (from packageorderapplydetail in _packageorderapplyexpressService.Query<Packageorderapplydetail>(t => t.Status == 1 && appliesIds.Contains(t.Packageorderapplyid))
                                  join package in _packageorderapplyexpressService.Query<Package>()
                                  on packageorderapplydetail.Packageid equals package.Id
                                  select new
                                  {
                                      PackageorderapplyId = packageorderapplydetail.Packageorderapplyid,
                                      Package = package
                                  }).ToListAsync();

            var packageids = packages.Select(t => t.Package.Id).ToList();
            var imageInfos = await (from image in _packageorderapplyexpressService.Query<Packageimage>(t => packageids.Contains(t.Packageid))
                                    select image
            ).ToListAsync();

            var vedioInfos = await (from vedio in _packageorderapplyexpressService.Query<Packagevideo>(t => packageids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            foreach (var item in orderApplyResponseList.List)
            {
                item.Orderstatusname = Enum.GetName(typeof(OrderApplyStatus), item.Orderstatus);
                item.Canclose = item.Orderstatus == (int)OrderApplyStatus.已发货;

                item.Flows = applynotes.Where(t => t.Packageorderapplyid == item.Id).Select(t => new OrderExpressFlowResponse()
                {
                    Applyid = t.Packageorderapplyid,
                    Addtime = t.Addtime,
                    Operator = t.Operator,
                    Operatorname = Enum.GetName(typeof(OrderApplyStatus), t.Operator)
                }).ToList();
                if (item.Orderstatus >= (int)OrderApplyStatus.申请打包)
                {
                    item.Packages = packages.Where(t => t.PackageorderapplyId == item.Id).Select(t => new OrderExpressPackageResponse()
                    {
                        PackageId = t.Package.Id,
                        Code = t.Package.Code,
                        PackageName = t.Package.Name,
                        Expressnumber = t.Package.Expressnumber,
                        Count = t.Package.Count,
                        Weight = t.Package.Weight,
                        Freightprice = t.Package.Freightprice??0
                    }).ToList();
                    foreach (var Detail in item.Packages)
                    {
                        Detail.Images = imageInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                        Detail.Videos = vedioInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                    }


                }
                if (item.Orderstatus >= (int)OrderApplyStatus.已发货)
                {
                    Packageorderapplyexpress packageorderapplyexpress = packageorderapplyexpresses.SingleOrDefault(t => t.Packageorderapplyid == item.Id);
                    item.ExpressId = packageorderapplyexpress.Id;
                    item.Remark = packageorderapplyexpress.Remark;
                    item.Mailcode = packageorderapplyexpress.Mailcode;
                    item.Price = packageorderapplyexpress.Price;
                    item.Freightprice = packageorderapplyexpress.Freightprice;
                    var currentAgent = agents.SingleOrDefault(t => t.Id == packageorderapplyexpress.Agentid);
                    item.Agentid = currentAgent.Id;
                    item.Agentname = currentAgent.Name;

                    var currentCourier = couriers.SingleOrDefault(t => t.Id == packageorderapplyexpress.Courierid);
                    item.Courierid = currentCourier.Id;
                    item.Couriername = currentCourier.Name;

                    var currentpackageorderapplyexpressdetails = packageorderapplyexpressdetails.Where(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id);
                    item.Details = currentpackageorderapplyexpressdetails.Select(ppp => new OrderExpressResponseDetail()
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
                        Price = ppp.Price
                    }).ToList();



                }

            }

            return RickWebResult.Success(orderApplyResponseList);
        }

        /// <summary>
        /// 签收
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromQuery] long id)
        {
            await _packageorderapplyexpressService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Packageorderapply packageorderapply = await _packageorderapplyexpressService.FindAsync<Packageorderapply>(id);
            if (packageorderapply.Orderstatus != (int)OrderApplyStatus.已发货)
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }
            packageorderapply.Orderstatus = (int)OrderApplyStatus.已签收;

            await _packageorderapplyexpressService.UpdateAsync(packageorderapply);

            Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            packageorderapplynote.Id = _idGenerator.NextId();
            packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            packageorderapplynote.Status = 1;
            packageorderapplynote.Adduser = UserInfo.Id;
            packageorderapplynote.Addtime = now;
            packageorderapplynote.Isclosed = 1;
            packageorderapplynote.Operator = (int)OrderApplyStatus.已签收;
            packageorderapplynote.Operatoruser = UserInfo.Id;
            await _packageorderapplyexpressService.AddAsync(packageorderapplynote);

            var packageorderapplydetails = await _packageorderapplyexpressService.Query<Packageorderapplydetail>(t => t.Packageorderapplyid == id).ToListAsync();
            foreach (Packageorderapplydetail packageorderapplydetail in packageorderapplydetails)
            {
                Packagenote packagenote = new Packagenote();
                packagenote.Id = _idGenerator.NextId();
                packagenote.Packageid = packageorderapplydetail.Packageid;
                packagenote.Status = 1;
                packagenote.Adduser = UserInfo.Id;
                packagenote.Addtime = now;
                packagenote.Isclosed = 1;
                packagenote.Operator = (int)PackageNoteStatus.已签收;
                packagenote.Operatoruser = UserInfo.Id;
                await _packageorderapplyexpressService.AddAsync(packagenote);
                Expressclaim expressclaim = await _packageorderapplyexpressService.FindAsync<Expressclaim>(packageorderapplydetail.Exclaimid);
                expressclaim.Status = (int)ExpressClaimStatus.已签收;
                await _packageorderapplyexpressService.UpdateAsync(expressclaim);
            }

            await _packageorderapplyexpressService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class OrderExpressResponse
        {
            public long Id { get; set; }
            public string Code { get; set; }

            public long ExpressId { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string NationName { get; set; }
            public long Agentid { get; set; }
            public string Agentname { get; set; }
            public long Courierid { get; set; }
            public string Couriername { get; set; }
            public long Addressid { get; set; }
            public string Name { get; set; }
            public string Contactnumber { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
            public DateTime Addtime { get; set; }
            public string Remark { get; set; }
            public decimal? Price { get; set; }
            public decimal? Freightprice { get; set; }
            public string Mailcode { get; set; }
            public int Orderstatus { get; set; }
            public bool Canclose { get; set; }

            public string Orderstatusname { get; set; }

            public List<OrderExpressResponseDetail> Details { get; set; }
            public List<OrderExpressFlowResponse> Flows { get; set; }
            public List<OrderExpressPackageResponse> Packages { get; set; }

        }

        public class OrderExpressPackageResponse
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
            public string Code { get; set; }
            public string Expressnumber { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Freightprice { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }
        public class OrderExpressFlowResponse
        {
            public long? Applyid { get; set; }
            public DateTime Addtime { get; set; }
            public int Operator { get; set; }
            public string Operatorname { get; set; }

        }
        public class OrderExpressResponseDetail
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
            public decimal? Remoteprice { get; set; }
            public bool Haselectrified { get; set; }
            public decimal? Packaddprice { get; set; }

            public decimal? Price { get; set; }
        }
        public class OrderExpressResponseList
        {
            public int Count { get; set; }
            public IList<OrderExpressResponse> List { get; set; }
        }


    }
}
