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
    /// 订单财务清账
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApplyAccountController : RickControllerBase
    {
        private readonly ILogger<OrderApplyAccountController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public OrderApplyAccountController(ILogger<OrderApplyAccountController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
        /// <param name="agentId"></param>
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
        public async Task<RickWebResult<OrderApplyAccountResponseList>> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] long? agentId, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string sendUserName, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (t.Orderstatus == (int)OrderApplyStatus.待发货 || t.Orderstatus == (int)OrderApplyStatus.已发货 || t.Orderstatus == (int)OrderApplyStatus.已签收)
                        && (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1 && (!agentId.HasValue || t.Agentid == agentId))
                        on order.Id equals packageorderapplyexpress.Packageorderapplyid
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
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
                        select new OrderApplyAccountResponse()
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
                            Price = packageorderapplyexpress.Price ?? 0,
                            Targetprice = packageorderapplyexpress.Targetprice ?? 0,
                            Freightprice = packageorderapplyexpress.Freightprice ?? 0,
                            Outnumber = packageorderapplyexpress.Outnumber,
                            Innernumber = packageorderapplyexpress.Innernumber,
                            Agentid = packageorderapplyexpress.Agentid,
                            Agentname = agent == null ? string.Empty : agent.Name,
                            Agentprice = packageorderapplyexpress.Agentprice ?? 0,
                            Localagentprice = packageorderapplyexpress.Localagentprice ?? 0,
                            Courierid = packageorderapplyexpress.Courierid,
                            Couriername = courier == null ? string.Empty : courier.Name,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Senduser = sysUser == null ? 0 : sysUser.Id,
                            Sendusername = sysUser == null ? string.Empty : sysUser.Name,
                            Sendtime = order.Sendtime,
                            Isagentpayed = order.Isagentpayed,
                            Agentpaytime = order.Agentpaytime,

                        };

            OrderApplyAccountResponseList orderApplyExpressList = new OrderApplyAccountResponseList();
            orderApplyExpressList.Count = await query.CountAsync();
            orderApplyExpressList.List = await query.OrderByDescending(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var userPayOrderids = orderApplyExpressList.List.Where(t => t.Ispayed == 1).Select(t => t.Id).ToList();
            var agentPayOrderids = orderApplyExpressList.List.Where(t => t.Isagentpayed == 1).Select(t => t.Id).ToList();

            var appuseraccountconsumes = await _packageOrderApplyService.QueryAsync<Appuseraccountconsume>(t => userPayOrderids.Contains((long)t.Orderid) && t.Status == 1);
            var agentfeeconsumes = await _packageOrderApplyService.QueryAsync<Agentfeeconsume>(t => agentPayOrderids.Contains((long)t.Orderid) && t.Status == 1);

            List<long> currenciesIds = new List<long>();
            List<long> userIds = new List<long>();
            foreach (var item in orderApplyExpressList.List)
            {
                if (item.Ispayed == 1)
                {
                    item.UserPay = appuseraccountconsumes.Where(t => t.Orderid == item.Id).Select(t => new UserPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Curencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.UserPay != null)
                    {
                        if (!currenciesIds.Contains(item.UserPay.Currencyid))
                        {
                            currenciesIds.Add(item.UserPay.Currencyid);
                        }
                        if (!userIds.Contains(item.UserPay.Userid))
                        {
                            userIds.Add(item.UserPay.Userid);
                        }
                    }
                }
                if (item.Isagentpayed == 1)
                {
                    item.AgentPay = agentfeeconsumes.Where(t => t.Orderid == item.Id).Select(t => new AgentPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Currencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.AgentPay != null)
                    {
                        if (!currenciesIds.Contains(item.AgentPay.Currencyid))
                        {
                            currenciesIds.Add(item.AgentPay.Currencyid);
                        }
                        if (!userIds.Contains(item.AgentPay.Userid))
                        {
                            userIds.Add(item.AgentPay.Userid);
                        }
                    }
                }
            }

            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => currenciesIds.Contains(t.Id));
            var useres = await _packageOrderApplyService.QueryAsync<Sysuser>(t => userIds.Contains(t.Id));

            foreach (var item in orderApplyExpressList.List)
            {
                if (item.UserPay != null)
                {
                    item.UserPay.Currencyname = currencies.Single(t => t.Id == item.UserPay.Currencyid).Name;
                    item.UserPay.Username = useres.Single(t => t.Id == item.UserPay.Userid).Name;
                }
                if (item.AgentPay != null)
                {
                    item.AgentPay.Currencyname = currencies.Single(t => t.Id == item.AgentPay.Currencyid).Name;
                    item.AgentPay.Username = useres.Single(t => t.Id == item.AgentPay.Userid).Name;
                }
            }
            
            return RickWebResult.Success(orderApplyExpressList);
        }

        /// <summary>
        /// 用户清账
        /// </summary>
        /// <param name="orderApplyExpressPutResquest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] OrderApplyAccountPutResquest orderApplyExpressPutResquest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            #region 注释
            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);//人民币
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);//美元

            //Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyExpressPutResquest.Id);

            var packageorderapplyes = await _packageOrderApplyService.QueryAsync<Packageorderapply>(t=> orderApplyExpressPutResquest.Ids.Contains(t.Id));
            int index = 0;
            foreach (Packageorderapply packageorderapply in packageorderapplyes)
            {
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
                if (index == 0 && appuser.Shareuser.HasValue && appuser.Shareuser != 0)
                {
                    var OldpackageorderapplyCount = await _packageOrderApplyService.CountAsync<Packageorderapply>(t => t.Appuser == appuser.Id && t.Ispayed == 1);
                    if (OldpackageorderapplyCount == 0)//首次消费
                    {
                        Syssetting shareAward = (await _packageOrderApplyService.QueryAsync<Syssetting>(t => t.Code == "001")).FirstOrDefault();
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
                packageorderapply.Paytime = now;
                packageorderapply.Ispayed = 1;
                await _packageOrderApplyService.UpdateAsync(packageorderapply);

                if (appuserdefaultaccount.Amount >= (decimal)packageorderapplyexpress.Price || appuserlocalaccount.Amount >= (decimal)packageorderapplyexpress.Targetprice)
                {
                    //美元账户充足
                    if (appuserdefaultaccount.Amount >= (decimal)packageorderapplyexpress.Price)
                    {
                        appuserdefaultaccount.Amount -= (decimal)packageorderapplyexpress.Price;

                        Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();

                        appuseraccountconsume.Id = _idGenerator.NextId();
                        appuseraccountconsume.Status = 1;
                        appuseraccountconsume.Adduser = UserInfo.Id;
                        appuseraccountconsume.Addtime = now;
                        appuseraccountconsume.Appuser = packageorderapply.Appuser;
                        appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Price;
                        appuseraccountconsume.Curencyid = appuserdefaultaccount.Currencyid;
                        appuseraccountconsume.Orderid = packageorderapply.Id;
                        await _packageOrderApplyService.UpdateAsync(appuserdefaultaccount);
                        await _packageOrderApplyService.AddAsync(appuseraccountconsume);
                    }
                    else if (appuserlocalaccount.Amount >= (decimal)packageorderapplyexpress.Targetprice)
                    {
                        appuserlocalaccount.Amount -= (decimal)packageorderapplyexpress.Targetprice;
                        await _packageOrderApplyService.UpdateAsync(appuserlocalaccount);
                        Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();
                        appuseraccountconsume.Id = _idGenerator.NextId();
                        appuseraccountconsume.Status = 1;
                        appuseraccountconsume.Adduser = UserInfo.Id;
                        appuseraccountconsume.Addtime = now;
                        appuseraccountconsume.Appuser = packageorderapply.Appuser;
                        appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Targetprice;
                        appuseraccountconsume.Curencyid = appuserlocalaccount.Currencyid;
                        appuseraccountconsume.Orderid = packageorderapply.Id;
                        await _packageOrderApplyService.AddAsync(appuseraccountconsume);
                    }
                }
                else
                {
                    await _packageOrderApplyService.RollBackAsync();
                    return RickWebResult.Error(new object(), 996, "用户余额不足");
                }
                
                if (index == 0 && consumeAwardMoney > 0)
                {
                    appuserdefaultaccount.Amount += consumeAwardMoney;

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
                index++;
            }
            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());

            #endregion
        }

        /// <summary>
        /// 代理商清账
        /// </summary>
        /// <param name="orderApplyExpressResquest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] OrderApplyAccountResquest orderApplyExpressResquest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyExpressResquest.Id);
            if (!((packageorderapply.Status == (int)OrderApplyStatus.已发货 || packageorderapply.Status == (int)OrderApplyStatus.已签收) && packageorderapply.Isagentpayed == 0))
            {
                RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }

            packageorderapply.Isagentpayed = 1;
            packageorderapply.Agentpaytime = now;
            packageorderapply.Lasttime = now;
            packageorderapply.Lastuser = UserInfo.Id;

            await _packageOrderApplyService.UpdateAsync(packageorderapply);
            Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.FindAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1);

            Agentfeeconsume agentfeeconsume = new Agentfeeconsume();

            agentfeeconsume.Id = _idGenerator.NextId();
            agentfeeconsume.Currencyid = orderApplyExpressResquest.Currencyid;
            agentfeeconsume.Agentid = (long)packageorderapplyexpress.Agentid;
            agentfeeconsume.Amount = orderApplyExpressResquest.Amount;
            agentfeeconsume.Orderid = packageorderapply.Id;
            agentfeeconsume.Status = 1;
            agentfeeconsume.Addtime = now;
            agentfeeconsume.Adduser = UserInfo.Id;

            await _packageOrderApplyService.AddAsync(agentfeeconsume);

            Agentfeeaccount agentfeeaccount = await _packageOrderApplyService.FindAsync<Agentfeeaccount>(t => t.Currencyid == agentfeeconsume.Currencyid && t.Agentid == agentfeeconsume.Agentid);
            if (agentfeeaccount == null)
            {
                agentfeeaccount = new Agentfeeaccount();
                agentfeeaccount.Id = _idGenerator.NextId();
                agentfeeaccount.Currencyid = agentfeeconsume.Currencyid;
                agentfeeaccount.Agentid = agentfeeconsume.Agentid;
                agentfeeaccount.Amount = -agentfeeconsume.Amount;
                agentfeeaccount.Status = 1;
                agentfeeaccount.Addtime = now;
                agentfeeaccount.Adduser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(agentfeeaccount);
            }
            else
            {
                agentfeeaccount.Amount -= agentfeeconsume.Amount;
                await _packageOrderApplyService.UpdateAsync(agentfeeaccount);
            }

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        public class OrderApplyAccountResquest
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
        }
        public class OrderApplyAccountResponse
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
            public sbyte Isagentpayed { get; set; }
            public DateTime? Agentpaytime { get; set; }

            public decimal? Price { get; set; }
            public decimal? Targetprice { get; set; }

            public decimal? Freightprice { get; set; }

            public string Outnumber { get; set; }
            public string Innernumber { get; set; }
            public long? Agentid { get; set; }
            public string Agentname { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public decimal? Localagentprice { get; set; }

            public decimal? Agentprice { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }
            public UserPay UserPay { get; set; }
            public AgentPay AgentPay { get; set; }
        }
        public class OrderApplyAccountResponseList
        {
            public int Count { get; set; }
            public List<OrderApplyAccountResponse> List { get; set; }

        }
        public class OrderApplyAccountPutResquest
        {
            public List<long> Ids { get; set; }
        }


    }
    public class UserPay
    {
        public DateTime Paytime { get; set; }
        public long Userid { get; set; }
        public string Username { get; set; }
        public long Currencyid { get; set; }
        public string Currencyname { get; set; }
        public decimal Amount { get; set; }
    }
    public class AgentPay
    {
        public DateTime Paytime { get; set; }
        public long Userid { get; set; }
        public string Username { get; set; }
        public long Currencyid { get; set; }
        public string Currencyname { get; set; }
        public decimal Amount { get; set; }
    }

}
