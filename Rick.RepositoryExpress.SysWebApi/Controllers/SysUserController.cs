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
    public class SysUserController : RickControllerBase
    {
        private readonly ILogger<SysUserController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public SysUserController(ILogger<SysUserController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }
        [HttpGet]
        public async Task<RickWebResult<SysUserResponseList>> Get([FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            SysUserResponseList sysUserResponseList = new SysUserResponseList();
            var query = from user in _sysuserService.Query<Sysuser>(t => (!status.HasValue || t.Status == status))
                        select new SysUserResponse()
                        {
                            Id = user.Id,
                            Mobile = user.Mobile,
                            Name = user.Name,
                            Addtime = user.Addtime,
                            Status = user.Status
                        };
            sysUserResponseList.Count = await query.CountAsync();
            sysUserResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            return RickWebResult.Success(sysUserResponseList);
        }
        public class SysUserResponse
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Name { get; set; }
            public DateTime Addtime { get; set; }
            public int Status { get; set; }
        }
        public class SysUserResponseList
        {
            public int Count { get; set; }
            public List<SysUserResponse> List { get; set; }
        }

    }
}
