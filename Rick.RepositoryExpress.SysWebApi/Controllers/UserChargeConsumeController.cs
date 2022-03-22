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
    /// 用户充值清账
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserChargeConsumeController : RickControllerBase
    {
        private readonly ILogger<UserChargeConsumeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public UserChargeConsumeController(ILogger<UserChargeConsumeController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询充值消费明细
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserChargeConsumeResponseList>> Get([FromQuery] long id, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from consume in _packageOrderApplyService.Query<Appuseraccountconsume>(t => t.Chargeid == id
                        )
                        join packageorderapply in _packageOrderApplyService.Query<Packageorderapply>()
                        on consume.Orderid equals packageorderapply.Id
                        select new UserChargeConsumeResponse()
                        {
                            Id = consume.Id,
                            Code = packageorderapply.Code,
                            Currencyid = (long)consume.Curencyid,
                            Amount = consume.Amount,
                            Paytime = packageorderapply.Paytime,
                        };

            UserChargeConsumeResponseList userChargeConsumeResponseList = new UserChargeConsumeResponseList();
            userChargeConsumeResponseList.Count = await query.CountAsync();
            userChargeConsumeResponseList.List = await query.OrderByDescending(t => t.Paytime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var currencyids = userChargeConsumeResponseList.List.Select(t => t.Currencyid).Distinct().ToList();
            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => currencyids.Contains(t.Id));

            foreach (var item in userChargeConsumeResponseList.List)
            {
                item.Currencyname = currencies.Single(t => t.Id == item.Currencyid).Name;
            }
            return RickWebResult.Success(userChargeConsumeResponseList);
        }

        /// <summary>
        /// 用户充值清账
        /// </summary>
        /// <param name="userChargeConsumeResquest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] UserChargeConsumeResquest userChargeConsumeResquest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            decimal remainAmount = 0;
            DateTime now = DateTime.Now;
            var packageorderapplies = await _packageOrderApplyService.QueryAsync<Packageorderapply>(t => userChargeConsumeResquest.Orders.Contains(t.Id));
            foreach (Packageorderapply packageorderapply in packageorderapplies)
            {
                if (packageorderapply.Orderstatus != (int)OrderApplyStatus.待发货 || packageorderapply.Ispayed == 1)
                {
                    return RickWebResult.Error<object>(null, 996, "包裹状态不正确");
                }
            }
            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);

            Appuseraccountcharge appuseraccountcharge = await _packageOrderApplyService.FindAsync<Appuseraccountcharge>(userChargeConsumeResquest.Id);
            if (appuseraccountcharge.Status != 1)
            {
                return RickWebResult.Error<object>(null, 996, "充值未审核");
            }
            if (appuseraccountcharge.Currencyid != localCurrency.Id && appuseraccountcharge.Currencyid != defaultCurrency.Id)
            {
                return RickWebResult.Error<object>(null, 996, "支付货币不支持");
            }
            Appuser appuser = await _packageOrderApplyService.FindAsync<Appuser>(appuseraccountcharge.Appuser);

            Appuseraccount appuseraccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == appuseraccountcharge.Appuser && t.Currencyid == appuseraccountcharge.Currencyid && t.Status == 1)).FirstOrDefault();

            int index = 0;
            foreach (Packageorderapply packageorderapply in packageorderapplies)
            {
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

                        Appuseraccountcharge appuseraccountchargeShare = new Appuseraccountcharge();
                        appuseraccountchargeShare.Id = _idGenerator.NextId();
                        appuseraccountchargeShare.Status = 1;
                        appuseraccountchargeShare.Adduser = UserInfo.Id;
                        appuseraccountchargeShare.Appuser = shareappuserdefaultaccount.Appuser;
                        appuseraccountchargeShare.Currencyid = shareappuserdefaultaccount.Currencyid;
                        appuseraccountchargeShare.Amount = shareAwardMoney;
                        appuseraccountchargeShare.Paytype = (int)PayType.活动赠送;
                        appuseraccountchargeShare.Addtime = now;
                        await _packageOrderApplyService.AddAsync(appuseraccountchargeShare);
                    }
                }

                Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
                packageorderapply.Paytime = now;
                packageorderapply.Ispayed = 1;
                await _packageOrderApplyService.UpdateAsync(packageorderapply);
                if (appuseraccountcharge.Currencyid == localCurrency.Id)//扣人民币
                {
                    if (appuseraccountcharge.Remainamount >= (decimal)packageorderapplyexpress.Targetprice)
                    {
                        appuseraccountcharge.Remainamount -= (decimal)packageorderapplyexpress.Targetprice;
                        appuseraccount.Amount -= (decimal)packageorderapplyexpress.Targetprice;

                        Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();
                        appuseraccountconsume.Id = _idGenerator.NextId();
                        appuseraccountconsume.Status = 1;
                        appuseraccountconsume.Adduser = UserInfo.Id;
                        appuseraccountconsume.Addtime = now;
                        appuseraccountconsume.Appuser = packageorderapply.Appuser;
                        appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Targetprice;
                        appuseraccountconsume.Curencyid = appuseraccountcharge.Currencyid;
                        appuseraccountconsume.Orderid = packageorderapply.Id;
                        appuseraccountconsume.Chargeid = appuseraccountcharge.Id;
                        await _packageOrderApplyService.UpdateAsync(appuseraccountcharge);
                        await _packageOrderApplyService.UpdateAsync(appuseraccount);
                        await _packageOrderApplyService.AddAsync(appuseraccountconsume);
                    }
                    else
                    {
                        await _packageOrderApplyService.RollBackAsync();
                        return RickWebResult.Error(new object(), 996, "用户余额不足");

                    }
                }
                else if (appuseraccountcharge.Currencyid == defaultCurrency.Id)//扣美元
                {
                    if (appuseraccountcharge.Remainamount >= (decimal)packageorderapplyexpress.Price)
                    {
                        appuseraccountcharge.Remainamount -= (decimal)packageorderapplyexpress.Price;
                        appuseraccount.Amount -= (decimal)packageorderapplyexpress.Price;

                        Appuseraccountconsume appuseraccountconsume = new Appuseraccountconsume();
                        appuseraccountconsume.Id = _idGenerator.NextId();
                        appuseraccountconsume.Status = 1;
                        appuseraccountconsume.Adduser = UserInfo.Id;
                        appuseraccountconsume.Addtime = now;
                        appuseraccountconsume.Appuser = packageorderapply.Appuser;
                        appuseraccountconsume.Amount = (decimal)packageorderapplyexpress.Price;
                        appuseraccountconsume.Curencyid = appuseraccountcharge.Currencyid;
                        appuseraccountconsume.Orderid = packageorderapply.Id;
                        appuseraccountconsume.Chargeid = appuseraccountcharge.Id;
                        await _packageOrderApplyService.UpdateAsync(appuseraccountcharge);
                        await _packageOrderApplyService.UpdateAsync(appuseraccount);
                        await _packageOrderApplyService.AddAsync(appuseraccountconsume);
                    }
                    else
                    {
                        await _packageOrderApplyService.RollBackAsync();
                        return RickWebResult.Error(new object(), 996, "用户余额不足");

                    }
                }

                if (index == 0 && consumeAwardMoney > 0)
                {
                    if (appuseraccount.Currencyid == defaultCurrency.Id)
                    {
                        appuseraccount.Amount += consumeAwardMoney;

                        Appuseraccountcharge appuseraccountchargeShare = new Appuseraccountcharge();
                        appuseraccountchargeShare.Id = _idGenerator.NextId();
                        appuseraccountchargeShare.Status = 1;
                        appuseraccountchargeShare.Adduser = UserInfo.Id;
                        appuseraccountchargeShare.Appuser = appuseraccount.Appuser;
                        appuseraccountchargeShare.Currencyid = appuseraccount.Currencyid;
                        appuseraccountchargeShare.Amount = consumeAwardMoney;
                        appuseraccountchargeShare.Addtime = now;
                        appuseraccountchargeShare.Paytype = (int)PayType.活动赠送;

                        await _packageOrderApplyService.AddAsync(appuseraccountchargeShare);

                    }
                    else
                    {
                        //美元 账户
                        Appuseraccount appuserdefaultaccount = (await _packageOrderApplyService.QueryAsync<Appuseraccount>(t => t.Appuser == packageorderapply.Appuser && t.Currencyid == defaultCurrency.Id && t.Status == 1)).FirstOrDefault();
                        appuserdefaultaccount.Amount += consumeAwardMoney;

                        Appuseraccountcharge appuseraccountchargeShare = new Appuseraccountcharge();
                        appuseraccountchargeShare.Id = _idGenerator.NextId();
                        appuseraccountchargeShare.Status = 1;
                        appuseraccountchargeShare.Adduser = UserInfo.Id;
                        appuseraccountchargeShare.Appuser = appuserdefaultaccount.Appuser;
                        appuseraccountchargeShare.Currencyid = appuserdefaultaccount.Currencyid;
                        appuseraccountchargeShare.Amount = consumeAwardMoney;
                        appuseraccountchargeShare.Addtime = now;
                        appuseraccountchargeShare.Paytype = (int)PayType.活动赠送;

                        await _packageOrderApplyService.AddAsync(appuseraccountchargeShare);

                    }
                }
                index++;
            }
            remainAmount = appuseraccountcharge.Remainamount;
            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success((object)remainAmount);
        }

        public class UserChargeConsumeResquest
        {
            public List<long> Orders { get; set; }
            public long Id { get; set; }
        }
        public class UserChargeConsumeResponse
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public decimal Amount { get; set; }

            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public DateTime? Paytime { get; set; }
        }

        public class UserChargeConsumeResponseList
        {
            public int Count { get; set; }
            public List<UserChargeConsumeResponse> List { get; set; }

        }

    }

}
