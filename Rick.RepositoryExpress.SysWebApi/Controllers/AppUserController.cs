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
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppUserResponseList>> Get([FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            AppUserResponseList appUserResponseList = new AppUserResponseList();
            var query = from user in _appuserService.Query<Appuser>(t => (!status.HasValue || t.Status == status))
                        select new AppUserResponse()
                        {
                            Id = user.Id,
                            Mobile = user.Mobile,
                            Countrycode = user.Countrycode,
                            Name = user.Name,
                            Headportrait = user.Headportrait,
                            Addtime = user.Addtime,
                            Status = user.Status
                        };
            appUserResponseList.Count = await query.CountAsync();
            appUserResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            return RickWebResult.Success(appUserResponseList);
        }

        public class AppUserResponse
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Countrycode { get; set; }
            public string Name { get; set; }
            public string Headportrait { get; set; }
            public DateTime Addtime { get; set; }
            public int Status { get; set; }
        }
        public class AppUserResponseList
        {
            public int Count { get; set; }
            public List<AppUserResponse> List { get; set; }
        }

    }
}
