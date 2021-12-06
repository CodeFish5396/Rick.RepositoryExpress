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
        /// <param name="id"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="sendUserName"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderApplyExpressResponseList>> Get([FromQuery] long? id, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string sendUserName, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (t.Orderstatus == (int)OrderApplyStatus.确认发货 || t.Orderstatus == (int)OrderApplyStatus.出货录单 || t.Orderstatus == (int)OrderApplyStatus.已发货)
                        && (!id.HasValue || t.Id == id)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join channeldetail in _packageOrderApplyService.Query<Channeldetail>(channel => 1 == 1)
                        on order.Channelid equals channeldetail.Id
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on channeldetail.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on channeldetail.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) ||t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Senduser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(sendUserName) || (sysUser != null && sysUser.Name == sendUserName))
                        select new OrderApplyExpressResponse()
                        {
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            Appuser = order.Appuser,
                            Appusercode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Paytime = order.Paytime,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Senduser = sysUser == null ? 0 : sysUser.Id,
                            Sendusername = sysUser == null ? string.Empty : sysUser.Name,
                            Sendtime = order.Sendtime
                        };

            OrderApplyExpressResponseList orderApplyExpressList = new OrderApplyExpressResponseList();
            orderApplyExpressList.Count = await query.CountAsync();
            orderApplyExpressList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            foreach (var item in orderApplyExpressList.List)
            {
                var orderid = item.Id;
                var packageIds = await (from package in _packageOrderApplyService.Query<Packageorderapplydetail>(t => orderid == t.Packageorderapplyid && t.Status == 1)
                                        select package.Packageid
                        ).ToListAsync();

                var packages = await (from package in _packageOrderApplyService.Query<Package>(t => packageIds.Contains(t.Id))
                                      select new OrderApplyExpressResponseDetail()
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

            packageorderapply.Lasttime = DateTime.Now;
            packageorderapply.Lastuser = UserInfo.Id;
            packageorderapply.Sendtime = packageorderapply.Lasttime;
            packageorderapply.Senduser = UserInfo.Id;

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
            public string Appusercode { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public DateTime? Paytime { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }
            public List<OrderApplyExpressResponseDetail> Details { get; set; }

        }
        public class OrderApplyExpressResponseDetail
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
        public class OrderApplyExpressResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyExpressResponse> List { get; set; }

        }
    }
}
