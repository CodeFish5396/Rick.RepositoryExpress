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
    /// APP用户管理
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AppUserController : RickControllerBase
    {
        private readonly ILogger<AppUserController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;
        private readonly RedisClientService _redisClientService;

        public AppUserController(ILogger<AppUserController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuserService = appuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// APP用户查询
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="countryName"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppUserResponseList>> Get([FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string countryName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            AppUserResponseList appUserResponseList = new AppUserResponseList();
            var query = from user in _appuserService.Query<Appuser>(t => (!status.HasValue || t.Status == status)
                        && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                        && (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                        && (string.IsNullOrEmpty(countryName) || t.Countryname == countryName)
                        && (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        )
                        select new AppUserResponse()
                        {
                            Id = user.Id,
                            Mobile = user.Mobile,
                            Usercode = user.Usercode,
                            Countrycode = user.Countrycode,
                            Name = user.Truename,
                            Headportrait = user.Headportrait,
                            Addtime = user.Addtime,
                            Cityname = user.Cityname,
                            Gender = user.Gender,
                            Birthdate = user.Birthdate,
                            Email = user.Email,
                            Nickname = user.Name,
                            Countryname = user.Countryname,
                            Address = user.Address,
                            Status = user.Status
                        };
            appUserResponseList.Count = await query.CountAsync();
            appUserResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            var userIds = appUserResponseList.List.Select(t => t.Id);
            var accounts = await _appuserService.QueryAsync<Appuseraccount>(t => userIds.Contains(t.Appuser) && t.Status == 1);
            var currencyids = accounts.Select(t => t.Currencyid);
            var currencies = await _appuserService.QueryAsync<Currency>(t => currencyids.Contains(t.Id) && t.Status == 1);
            foreach (var appuserItem in appUserResponseList.List)
            {
                appuserItem.Accounts = (from account in accounts
                                        join currency in currencies
                                        on account.Currencyid equals currency.Id
                                        where account.Appuser == appuserItem.Id
                                        select new AppUserAccountResponse() { 
                                            Id = account.Id,
                                            Currencyid = account.Currencyid,
                                            Currencyname = currency.Name,
                                            Amount = account.Amount
                                        }).ToList();
            }
            return RickWebResult.Success(appUserResponseList);
        }

        public class AppUserResponse
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Usercode { get; set; }
            public string Countrycode { get; set; }
            public string Countryname { get; set; }
            public string Name { get; set; }
            public string Nickname { get; set; }
            public string Headportrait { get; set; }
            public DateTime Addtime { get; set; }
            public string Cityname { get; set; }
            public string Gender { get; set; }
            public DateTime? Birthdate { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public int Status { get; set; }
            public List<AppUserAccountResponse> Accounts { get; set; }
        }
        public class AppUserAccountResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }

        }

        public class AppUserResponseList
        {
            public int Count { get; set; }
            public List<AppUserResponse> List { get; set; }
        }

    }
}
