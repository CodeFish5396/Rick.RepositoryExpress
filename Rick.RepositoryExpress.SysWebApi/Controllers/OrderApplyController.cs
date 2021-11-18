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
        public async Task<RickWebResult<OrderApplyResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string expressNumber, [FromQuery] long? userId, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status) && (t.Orderstatus == (int)OrderApplyStatus.发起申请))
                        join channeldetail in _packageOrderApplyService.Query<Channeldetail>(channel => 1 == 1)
                        on order.Channelid equals channeldetail.Id
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on channeldetail.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on channeldetail.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t=>true)
                        on order.Appuser equals appuser.Id
                        select new OrderApplyResponse()
                        {
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            Appuser = order.Appuser,
                            AppuserCode = appuser.Usercode,
                            Appusername = appuser.Name,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed,
                            Paytime = order.Paytime,
                            Status = order.Status,
                            Addtime = order.Addtime
                        };

            OrderApplyResponseList orderApplyResponseList = new OrderApplyResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();
            

            foreach (var item in orderApplyResponseList.List)
            {
                var orderid = item.Id;
                var packageIds = await (from package in _packageOrderApplyService.Query<Packageorderapplydetail>(t => orderid == t.Id && t.Status == 1)
                                        select package.Id
                        ).ToListAsync();

                var packages = await (from package in _packageOrderApplyService.Query<Package>(t => packageIds.Contains(t.Id))
                                      select new OrderApplyResponseDetail()
                                      {
                                          PackageId = package.Id,
                                          PackageName = package.Name,
                                          Expressnumber = package.Expressnumber,
                                          Location = package.Location,
                                          Name = package.Name,
                                          Count = package.Count,
                                          Weight = package.Weight
                                      }
                            ).ToListAsync();
                item.Details = packages;
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
            }
            
            //orderApplyResponseList.List

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

            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyRequest.Id);
            packageorderapply.Orderstatus = (int)OrderApplyStatus.出货录单;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);

            Packageorderapplyexpress packageorderapplyexpress = new Packageorderapplyexpress();
            DateTime now = DateTime.Now;
            packageorderapplyexpress.Id = _idGenerator.NextId();
            packageorderapplyexpress.Packageorderapplyid = packageorderapply.Id;
            packageorderapplyexpress.Remark = orderApplyRequest.Remark;
            packageorderapplyexpress.Count = orderApplyRequest.Count;
            packageorderapplyexpress.Weight = orderApplyRequest.Weight;
            packageorderapplyexpress.Mailcode = orderApplyRequest.Mailcode;
            packageorderapplyexpress.Customprice = orderApplyRequest.Customprice;
            packageorderapplyexpress.Sueprice = orderApplyRequest.Sueprice;
            packageorderapplyexpress.Overlengthprice = orderApplyRequest.Overlengthprice;
            packageorderapplyexpress.Overweightprice = orderApplyRequest.Overweightprice;
            packageorderapplyexpress.Oversizeprice = orderApplyRequest.Oversizeprice;
            packageorderapplyexpress.Paperprice = orderApplyRequest.Paperprice;
            packageorderapplyexpress.Boxprice = orderApplyRequest.Boxprice;
            packageorderapplyexpress.Bounceprice = orderApplyRequest.Bounceprice;
            packageorderapplyexpress.Price = orderApplyRequest.Price;
            packageorderapplyexpress.Status = 1;
            packageorderapplyexpress.Addtime = now;
            packageorderapplyexpress.Lasttime = now;
            packageorderapplyexpress.Adduser = UserInfo.Id;
            packageorderapplyexpress.Lastuser = UserInfo.Id;
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
                await _packageOrderApplyService.AddAsync(packageorderapplyexpressdetail);
            }

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class OrderApplyRequest
        {
            public long Id { get; set; }
            public string Remark { get; set; }
            public int? Count { get; set; }
            public decimal? Weight { get; set; }
            public string Mailcode { get; set; }
            public string Customprice { get; set; }
            public decimal? Sueprice { get; set; }
            public decimal? Overlengthprice { get; set; }
            public decimal? Overweightprice { get; set; }
            public decimal? Oversizeprice { get; set; }
            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Price { get; set; }
            public IList<OrderApplyRequestdetail> Details { get; set; }
        }
        public class OrderApplyRequestdetail
        {
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Volumeweight { get; set; }

        }
        public class OrderApplyResponse
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public long Appuser { get; set; }
            public string AppuserCode { get; set; }

            public string Appusername { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public List<OrderApplyResponseDetail> Details { get; set; }
        }
        public class OrderApplyResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }

        public class OrderApplyResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyResponse> List { get; set; }
        }

    }
}
