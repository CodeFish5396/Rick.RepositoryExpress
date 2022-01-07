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
        /// <param name="code"></param>
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
        public async Task<RickWebResult<OrderApplyExpressResponseList>> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string sendUserName, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (t.Orderstatus == (int)OrderApplyStatus.待发货 || t.Orderstatus == (int)OrderApplyStatus.已发货 || t.Orderstatus == (int)OrderApplyStatus.已签收)
                        && (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1)
                        on order.Id equals packageorderapplyexpress.Packageorderapplyid
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join address in _packageOrderApplyService.Query<Appuseraddress>()
                        on order.Addressid equals address.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Senduser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        join courier in _packageOrderApplyService.Query<Courier>()
                        on packageorderapplyexpress.Courierid equals courier.Id
                        into courierTmp
                        from courier in courierTmp.DefaultIfEmpty()
                        join agent in _packageOrderApplyService.Query<Agent>()
                        on packageorderapplyexpress.Agentid equals agent.Id
                        into agentTmp
                        from agent in agentTmp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(sendUserName) || (sysUser != null && sysUser.Name == sendUserName))
                        select new OrderApplyExpressResponse()
                        {
                            Id = order.Id,
                            Expressid = packageorderapplyexpress.Id,
                            Code = order.Code,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            AddressName = address.Name,
                            AddressContactnumber = address.Contactnumber,
                            AddressRegion = address.Region,
                            AddressAddress = address.Address,
                            Appuser = order.Appuser,
                            Appusercode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed ?? 0,
                            Paytime = order.Paytime,
                            Price = packageorderapplyexpress.Price,
                            Freightprice = packageorderapplyexpress.Freightprice ?? 0,
                            Outnumber = packageorderapplyexpress.Outnumber,
                            Innernumber = packageorderapplyexpress.Innernumber,
                            Agentid = packageorderapplyexpress.Agentid,
                            Agentname = agent == null ? string.Empty : agent.Name,
                            Agentprice = packageorderapplyexpress.Agentprice,
                            Courierid = packageorderapplyexpress.Courierid,
                            Couriername = courier == null ? string.Empty : courier.Name,
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
                                          PackageCode = package.Code,
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

                var channelDetails = await (from cd in _packageOrderApplyService.Query<Channeldetail>(t => t.Nationid == item.Nationid && t.Channelid == item.Channelid && t.Status == 1)
                                            join agent in _packageOrderApplyService.Query<Agent>(t => t.Status == 1)
                                            on cd.Agentid equals agent.Id
                                            select new OrderApplyExpressResponseAgentDetail()
                                            {
                                                AgentId = agent.Id,
                                                AgentName = agent.Name
                                            }).ToListAsync();
                item.Agents = channelDetails;
            }

            return RickWebResult.Success(orderApplyExpressList);
        }

        /// <summary>
        /// 清账
        /// </summary>
        /// <param name="orderApplyExpressPutResquest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] OrderApplyExpressPutResquest orderApplyExpressPutResquest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            #region 注释
            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);//人民币
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);//美元
            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyExpressPutResquest.Id);
            if (packageorderapply.Status != (int)OrderApplyStatus.待发货 || packageorderapply.Ispayed == 1)
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }
            Appuser appuser = await _packageOrderApplyService.FindAsync<Appuser>(packageorderapply.Appuser);


            //RMB 账户
            Appuseraccount appuserlocalaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == packageorderapply.Appuser && t.Currencyid == localCurrency.Id && t.Status == 1)).FirstOrDefault();
            //美元 账户
            Appuseraccount appuserdefaultaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == packageorderapply.Appuser && t.Currencyid == defaultCurrency.Id && t.Status == 1)).FirstOrDefault();
            if (appuserlocalaccount == null || appuserdefaultaccount == null)
            {
                if (appuserlocalaccount == null)
                {
                    appuserlocalaccount = new Appuseraccount();
                    appuserlocalaccount.Id = _idGenerator.NextId();
                    appuserlocalaccount.Status = 1;
                    appuserlocalaccount.Adduser = UserInfo.Id;
                    appuserlocalaccount.Lastuser = UserInfo.Id;
                    appuserlocalaccount.Addtime = now;
                    appuserlocalaccount.Lasttime = now;
                    appuserlocalaccount.Appuser = packageorderapply.Appuser;
                    appuserlocalaccount.Amount = 0;
                    appuserlocalaccount.Currencyid = localCurrency.Id;

                    await _packageOrderApplyService.AddAsync(appuserlocalaccount);
                }

                if (appuserdefaultaccount == null)
                {
                    appuserdefaultaccount = new Appuseraccount();
                    appuserdefaultaccount.Id = _idGenerator.NextId();
                    appuserdefaultaccount.Status = 1;
                    appuserdefaultaccount.Adduser = UserInfo.Id;
                    appuserdefaultaccount.Lastuser = UserInfo.Id;
                    appuserdefaultaccount.Addtime = now;
                    appuserdefaultaccount.Lasttime = now;
                    appuserdefaultaccount.Appuser = packageorderapply.Appuser;
                    appuserdefaultaccount.Amount = 0;
                    appuserdefaultaccount.Currencyid = defaultCurrency.Id;

                    await _packageOrderApplyService.AddAsync(appuserdefaultaccount);
                }
                //await _packageOrderApplyService.CommitAsync();
            }

            decimal consumeAwardMoney = 0;
            if (appuser.Shareuser.HasValue && appuser.Shareuser != 0)
            {
                var OldpackageorderapplyCount = await _packageOrderApplyService.CountAsync<Packageorderapply>(t=>t.Appuser == appuser.Id && t.Ispayed == 1);
                if (OldpackageorderapplyCount == 0)//首次消费
                {
                    Syssetting shareAward = (await _packageOrderApplyService.QueryAsync<Syssetting>(t=>t.Code == "001")).FirstOrDefault();
                    Syssetting consumeAward = (await _packageOrderApplyService.QueryAsync<Syssetting>(t => t.Code == "002")).FirstOrDefault();
                    consumeAwardMoney = Convert.ToDecimal(consumeAward.Value);
                    Appuser shareUppuser = await _packageOrderApplyService.FindAsync<Appuser>((long)appuser.Shareuser);

                    //RMB 账户
                    Appuseraccount shareappuserlocalaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == shareUppuser.Id && t.Currencyid == localCurrency.Id && t.Status == 1)).FirstOrDefault();
                    //美元 账户
                    Appuseraccount shareappuserdefaultaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == shareUppuser.Id && t.Currencyid == defaultCurrency.Id && t.Status == 1)).FirstOrDefault();
                    if (shareappuserlocalaccount == null || shareappuserdefaultaccount == null)
                    {
                        if (shareappuserlocalaccount == null)
                        {
                            shareappuserlocalaccount = new Appuseraccount();
                            shareappuserlocalaccount.Id = _idGenerator.NextId();
                            shareappuserlocalaccount.Status = 1;
                            shareappuserlocalaccount.Adduser = UserInfo.Id;
                            shareappuserlocalaccount.Lastuser = UserInfo.Id;
                            shareappuserlocalaccount.Addtime = now;
                            shareappuserlocalaccount.Lasttime = now;
                            shareappuserlocalaccount.Appuser = shareUppuser.Id;
                            shareappuserlocalaccount.Amount = 0;
                            shareappuserlocalaccount.Currencyid = localCurrency.Id;

                            await _packageOrderApplyService.AddAsync(shareappuserlocalaccount);
                        }

                        if (shareappuserdefaultaccount == null)
                        {
                            shareappuserdefaultaccount = new Appuseraccount();
                            shareappuserdefaultaccount.Id = _idGenerator.NextId();
                            shareappuserdefaultaccount.Status = 1;
                            shareappuserdefaultaccount.Adduser = UserInfo.Id;
                            shareappuserdefaultaccount.Lastuser = UserInfo.Id;
                            shareappuserdefaultaccount.Addtime = now;
                            shareappuserdefaultaccount.Lasttime = now;
                            shareappuserdefaultaccount.Appuser = shareUppuser.Id;
                            shareappuserdefaultaccount.Amount = 0;
                            shareappuserdefaultaccount.Currencyid = defaultCurrency.Id;

                            await _packageOrderApplyService.AddAsync(shareappuserdefaultaccount);
                        }
                        //await _packageOrderApplyService.CommitAsync();
                    }
                    decimal shareAwardMoney = Convert.ToDecimal(shareAward.Value);
                    shareappuserdefaultaccount.Amount += shareAwardMoney;
                    await _packageOrderApplyService.UpdateAsync(shareappuserdefaultaccount);

                    Appuseraccountcharge appuseraccountcharge = new Appuseraccountcharge();
                    appuseraccountcharge.Id = _idGenerator.NextId();
                    appuseraccountcharge.Status = 1;
                    appuseraccountcharge.Adduser = UserInfo.Id;
                    appuseraccountcharge.Appuser = shareappuserdefaultaccount.Appuser;
                    appuseraccountcharge.Currencyid = shareappuserdefaultaccount.Currencyid;
                    appuseraccountcharge.Amount = shareAwardMoney;
                    appuseraccountcharge.Paytype = (int)PayType.活动赠送;
                    appuseraccountcharge.Addtime = now;
                    await _packageOrderApplyService.AddAsync(appuseraccountcharge);
                }
            }

            Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
            packageorderapply.Paytime = DateTime.Now;
            packageorderapply.Ispayed = 1;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);
            decimal remainPrice = 0;
            appuserdefaultaccount.Amount -= (decimal)packageorderapplyexpress.Price;
            if (appuserdefaultaccount.Amount <= 0)
            {
                remainPrice = -appuserdefaultaccount.Amount;
                appuserdefaultaccount.Amount = 0;
            }

            if (remainPrice > 0)
            {
                remainPrice = remainPrice * (decimal)(packageorderapplyexpress.Currencychangerate ?? 1);
                appuserlocalaccount.Amount -= remainPrice;
                if (appuserlocalaccount.Amount <= 0)
                {
                    await _packageOrderApplyService.RollBackAsync();
                    return RickWebResult.Error(new object(), 996, "您的余额不足");
                }
            }
            await _packageOrderApplyService.UpdateAsync(appuserdefaultaccount);

            Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();

            appuseraccountconsume.Id = _idGenerator.NextId();
            appuseraccountconsume.Status = 1;
            appuseraccountconsume.Adduser = UserInfo.Id;
            appuseraccountconsume.Addtime = now;
            appuseraccountconsume.Appuser = packageorderapply.Appuser;
            appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Price;

            await _packageOrderApplyService.AddAsync(appuseraccountconsume);

            if (consumeAwardMoney > 0)
            {
                appuserdefaultaccount.Amount += consumeAwardMoney;

                await _packageOrderApplyService.UpdateAsync(appuseraccountconsume);

                Appuseraccountcharge appuseraccountcharge = new Appuseraccountcharge();
                appuseraccountcharge.Id = _idGenerator.NextId();
                appuseraccountcharge.Status = 1;
                appuseraccountcharge.Adduser = UserInfo.Id;
                appuseraccountcharge.Appuser = appuserdefaultaccount.Appuser;
                appuseraccountcharge.Currencyid = appuserdefaultaccount.Currencyid;
                appuseraccountcharge.Amount = consumeAwardMoney;
                appuseraccountcharge.Addtime = now;
                appuseraccountcharge.Paytype = (int)PayType.活动赠送;

                await _packageOrderApplyService.AddAsync(appuseraccountcharge);
            }

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());

            #endregion
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
            if (packageorderapply.Status != (int)OrderApplyStatus.待发货 || packageorderapply.Ispayed != 1)
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }

            packageorderapply.Orderstatus = (int)OrderApplyStatus.已发货;

            packageorderapply.Lasttime = DateTime.Now;
            packageorderapply.Lastuser = UserInfo.Id;
            packageorderapply.Sendtime = packageorderapply.Lasttime;
            packageorderapply.Senduser = UserInfo.Id;

            await _packageOrderApplyService.UpdateAsync(packageorderapply);

            Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
            packageorderapplynote.Id = _idGenerator.NextId();
            packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
            packageorderapplynote.Status = 1;
            packageorderapplynote.Adduser = UserInfo.Id;
            packageorderapplynote.Addtime = now;
            packageorderapplynote.Isclosed = 0;
            packageorderapplynote.Operator = (int)OrderApplyStatus.已发货;
            packageorderapplynote.Operatoruser = UserInfo.Id;
            await _packageOrderApplyService.AddAsync(packageorderapplynote);

            var details = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id);
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
            packageorderapplyexpress.Agentid = orderApplyExpressResquest.Agentid;
            packageorderapplyexpress.Courierid = orderApplyExpressResquest.Courierid;
            packageorderapplyexpress.Outnumber = orderApplyExpressResquest.Outnumber;
            packageorderapplyexpress.Agentprice = orderApplyExpressResquest.Agentprice;
            Courier courier = await _packageOrderApplyService.FindAsync<Courier>(orderApplyExpressResquest.Courierid);
            packageorderapplyexpress.Couriercode = courier.Code;
            await _packageOrderApplyService.UpdateAsync(packageorderapplyexpress);

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        public class OrderApplyExpressResquest
        {
            public long Id { get; set; }
            public long Agentid { get; set; }
            public long Courierid { get; set; }
            public string Outnumber { get; set; }
            public decimal? Agentprice { get; set; }
        }
        public class OrderApplyExpressResponse
        {
            public string Code { get; set; }
            public long Id { get; set; }
            public long Expressid { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public string AddressName { get; set; }
            public string AddressContactnumber { get; set; }
            public string AddressRegion { get; set; }
            public string AddressAddress { get; set; }

            public long Appuser { get; set; }
            public string Appusername { get; set; }
            public string Appusercode { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public decimal? Price { get; set; }
            public decimal? Freightprice { get; set; }

            public string Outnumber { get; set; }
            public string Innernumber { get; set; }
            public long? Agentid { get; set; }
            public string Agentname { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public decimal? Agentprice { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }
            public List<OrderApplyExpressResponseAgentDetail> Agents { get; set; }
            public List<OrderApplyExpressResponseDetail> Details { get; set; }
            public OrderApplyExpressPostResponse PostedDetails { get; set; }


        }
        public class OrderApplyExpressResponseAgentDetail
        {
            public long AgentId { get; set; }
            public string AgentName { get; set; }
        }

        public class OrderApplyExpressResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageCode { get; set; }
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
        public class OrderApplyExpressPutResquest
        {
            public long Id { get; set; }
        }
        public class OrderApplyExpressPostResponse
        {
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public IList<OrderApplyExpressPostResponsedetail> Details { get; set; }
        }
        public class OrderApplyExpressPostResponsedetail
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

            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Price { get; set; }
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Volumeweight { get; set; }
            public List<OrderApplyExpressPostResponseDetail> Packages { get; set; }
        }
        public class OrderApplyExpressPostResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
        }
    }
}
