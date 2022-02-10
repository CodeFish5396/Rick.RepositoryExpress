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
    /// 用户充值
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UseraccountchargeController : RickControllerBase
    {
        private readonly ILogger<UseraccountchargeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraccountService _appuseraccountService;
        private readonly RedisClientService _redisClientService;
        private string accountSubjectCode = "100";

        public UseraccountchargeController(ILogger<UseraccountchargeController> logger, IAppuseraccountService appuseraccountService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraccountService = appuseraccountService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户充值
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="currencyid"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserAccountChargeResponseList>> Get([FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime,[FromQuery] int? status, [FromQuery] long? currencyid, [FromQuery] long? userid, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from accountcharge in _appuseraccountService.Query<Appuseraccountcharge>(t => (!status.HasValue || t.Status == status) && (!currencyid.HasValue || t.Currencyid == currencyid) && (!userid.HasValue || t.Appuser == userid) && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime<= endTime))
                        join user in _appuseraccountService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode) && (string.IsNullOrEmpty(userName) || t.Truename == userName) && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile))
                        on accountcharge.Appuser equals user.Id
                        join currency in _appuseraccountService.Query<Currency>(t => true)
                        on accountcharge.Currencyid equals currency.Id
                        into temp
                        from tc in temp.DefaultIfEmpty()
                        select new
                        {
                            Id = accountcharge.Id,
                            Userid = accountcharge.Appuser,
                            Username = user.Name,
                            Usercode = user.Usercode,
                            Usermobile = user.Mobile,
                            Currencyid = accountcharge.Currencyid,
                            CurrencyName = tc == null ? string.Empty : tc.Name,
                            Amount = accountcharge.Amount,
                            Status = accountcharge.Status,
                            Addtime = accountcharge.Addtime,
                            Paytype = accountcharge.Paytype
                        };
            int count = await query.CountAsync();

            var queryGroup = from q in query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize)
                             join image in _appuseraccountService.Query<Appuseraccountchargeimage>(t => t.Status == 1)
                             on q.Id equals image.Appuseraccountchargeid
                             into imageTemp
                             from image in imageTemp.DefaultIfEmpty()
                             select new
                             {
                                 Id = q.Id,
                                 Userid = q.Userid,
                                 Username = q.Username,
                                 Usercode = q.Usercode,
                                 Usermobile = q.Usermobile,
                                 Paytype = q.Paytype,
                                 Currencyid = q.Currencyid,
                                 CurrencyName = q.CurrencyName,
                                 Amount = q.Amount,
                                 Status = q.Status,
                                 Addtime = q.Addtime,

                                 FileId = image == null ? 0 : image.Fileinfoid
                             };

            var queryR = from r in (await queryGroup.ToListAsync())
                         group r by new { r.Id, r.Userid, r.Username,r.Usercode,r.Usermobile ,r.Currencyid, r.CurrencyName, r.Amount, r.Status,r.Addtime,r.Paytype };

            UserAccountChargeResponseList userAccountResponseList = new UserAccountChargeResponseList();
            userAccountResponseList.List = new List<UserAccountChargeResponse>();

            foreach (var r in queryR)
            {
                UserAccountChargeResponse userAccountChargeResponse = new UserAccountChargeResponse();
                userAccountChargeResponse.Images = new List<long>();
                userAccountChargeResponse.Id = r.Key.Id;
                userAccountChargeResponse.Userid = r.Key.Userid;
                userAccountChargeResponse.Username = r.Key.Username;
                userAccountChargeResponse.Usercode = r.Key.Usercode;
                userAccountChargeResponse.Usermobile = r.Key.Usermobile;
                userAccountChargeResponse.Currencyid = r.Key.Currencyid;
                userAccountChargeResponse.CurrencyName = r.Key.CurrencyName;
                userAccountChargeResponse.Amount = r.Key.Amount;
                userAccountChargeResponse.Addtime = r.Key.Addtime;
                userAccountChargeResponse.Status = r.Key.Status;
                userAccountChargeResponse.Paytype = r.Key.Paytype;
                foreach (var image in r)
                {
                    if (image.FileId != 0)
                    {
                        userAccountChargeResponse.Images.Add(image.FileId);
                    }
                }
                userAccountResponseList.List.Add(userAccountChargeResponse);
            }
            userAccountResponseList.Count = count;
            return RickWebResult.Success(userAccountResponseList);

        }

        /// <summary>
        /// 用户充值
        /// </summary>
        /// <param name="userAccountChargeRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] UserAccountChargeRequest userAccountChargeRequest)
        {
            await _appuseraccountService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            //Account account = new Account();
            //account.Id = _idGenerator.NextId();
            //account.Currencyid = userAccountChargeRequest.Currencyid;
            //account.Amount = userAccountChargeRequest.Amount;
            //account.Status = 1;
            //account.Addtime = now;
            //account.Adduser = UserInfo.Id;
            //account.Subjectcode = accountSubjectCode;
            //account.Direction = 1;
            //await _appuseraccountService.AddAsync(account);

            Appuseraccountcharge appuseraccountcharge = new Appuseraccountcharge();
            appuseraccountcharge.Id = _idGenerator.NextId();
            appuseraccountcharge.Status = 1;
            appuseraccountcharge.Adduser = UserInfo.Id;
            appuseraccountcharge.Appuser = userAccountChargeRequest.Userid;
            appuseraccountcharge.Currencyid = userAccountChargeRequest.Currencyid;
            appuseraccountcharge.Amount = userAccountChargeRequest.Amount;
            appuseraccountcharge.Addtime = now;
            appuseraccountcharge.Paytype = userAccountChargeRequest.PayType;
            await _appuseraccountService.AddAsync(appuseraccountcharge);

            Appuseraccount appuseraccount = (await _appuseraccountService.QueryAsync<Appuseraccount>(t => t.Appuser == userAccountChargeRequest.Userid && t.Status == 1 && t.Currencyid == userAccountChargeRequest.Currencyid)).SingleOrDefault();
            if (appuseraccount == null)
            {
                appuseraccount = new Appuseraccount();
                appuseraccount.Id = _idGenerator.NextId();
                appuseraccount.Status = 1;
                appuseraccount.Adduser = UserInfo.Id;
                appuseraccount.Lastuser = UserInfo.Id;
                appuseraccount.Addtime = now;
                appuseraccount.Lasttime = now;
                appuseraccount.Appuser = userAccountChargeRequest.Userid;
                appuseraccount.Amount = appuseraccountcharge.Amount;
                appuseraccount.Currencyid = appuseraccountcharge.Currencyid;

                await _appuseraccountService.AddAsync(appuseraccount);
            }
            else
            {
                appuseraccount.Lastuser = UserInfo.Id;
                appuseraccount.Lasttime = now;
                appuseraccount.Amount += appuseraccountcharge.Amount;
                await _appuseraccountService.UpdateAsync(appuseraccount);
            }
            await _appuseraccountService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 用户充值审核
        /// </summary>
        /// <param name="userAccountPatchRequest"></param>
        /// <returns></returns>
        [HttpPatch]
        public async Task<RickWebResult<object>> Patch([FromBody] UserAccountPatchRequest userAccountPatchRequest)
        {
            await _appuseraccountService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Account account = new Account();
            account.Id = _idGenerator.NextId();
            account.Currencyid = userAccountPatchRequest.Currencyid;
            account.Amount = userAccountPatchRequest.Amount;
            account.Status = 1;
            account.Addtime = now;
            account.Adduser = UserInfo.Id;
            account.Subjectcode = accountSubjectCode;
            account.Direction = 1;
            await _appuseraccountService.AddAsync(account);


            Appuseraccountcharge appuseraccountcharge = await _appuseraccountService.FindAsync<Appuseraccountcharge>(userAccountPatchRequest.Id); ;
            appuseraccountcharge.Status = 1;
            appuseraccountcharge.Amount = userAccountPatchRequest.Amount;
            appuseraccountcharge.Currencyid = userAccountPatchRequest.Currencyid;
            appuseraccountcharge.Accountid = account.Id;
            appuseraccountcharge.Paytype = userAccountPatchRequest.PayType;

            await _appuseraccountService.UpdateAsync(appuseraccountcharge);
            Appuseraccount appuseraccount = (await _appuseraccountService.QueryAsync<Appuseraccount>(t => t.Appuser == appuseraccountcharge.Appuser && t.Currencyid == userAccountPatchRequest.Currencyid && t.Status == 1)).SingleOrDefault();
            if (appuseraccount == null)
            {
                appuseraccount = new Appuseraccount();
                appuseraccount.Id = _idGenerator.NextId();
                appuseraccount.Status = 1;
                appuseraccount.Adduser = UserInfo.Id;
                appuseraccount.Lastuser = UserInfo.Id;
                appuseraccount.Currencyid = userAccountPatchRequest.Currencyid;
                appuseraccount.Addtime = now;
                appuseraccount.Lasttime = now;
                appuseraccount.Appuser = appuseraccountcharge.Appuser;
                appuseraccount.Amount = appuseraccountcharge.Amount;
                await _appuseraccountService.AddAsync(appuseraccount);
            }
            else
            {
                appuseraccount.Lastuser = UserInfo.Id;
                appuseraccount.Lasttime = now;
                appuseraccount.Amount += appuseraccountcharge.Amount;
                await _appuseraccountService.UpdateAsync(appuseraccount);
            }

            await _appuseraccountService.CommitAsync();

            return RickWebResult.Success(new object());

        }

        public class UserAccountChargeResponse
        {
            public long Id { get; set; }

            public long Userid { get; set; }
            public string Username { get; set; }
            public string Usercode { get; set; }
            public string Usermobile { get; set; }

            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public decimal Amount { get; set; }
            public int Paytype { get; set; }

            public DateTime Addtime { get; set; }
            public int Status { get; set; }
            public List<long> Images { get; set; }
        }
        public class UserAccountChargeRequest
        {
            public long Userid { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public int PayType { get; set; }
        }
        public class UserAccountPatchRequest
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public int PayType { get; set; }
        }

        public class UserAccountChargeResponseList
        {
            public int Count { get; set; }
            public List<UserAccountChargeResponse> List { get; set; }

        }


    }
}
