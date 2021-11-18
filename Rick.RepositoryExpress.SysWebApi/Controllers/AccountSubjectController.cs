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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountSubjectController : RickControllerBase
    {
        private readonly ILogger<AccountSubjectController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAccountsubjectService _accountsubjectService;
        private readonly RedisClientService _redisClientService;

        public AccountSubjectController(ILogger<AccountSubjectController> logger, IAccountsubjectService accountsubjectService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _accountsubjectService = accountsubjectService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询会计科目
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Admin]
        public async Task<RickWebResult<AccountSubjectResponseList>> Get()
        {
            AccountSubjectResponseList accountSubjectResponseList = new AccountSubjectResponseList();
            var query = from accountSubject in _accountsubjectService.Query<Accountsubject>()
                        select new AccountSubjectResponse() { 
                            Id = accountSubject.Id,
                            Name = accountSubject.Name,
                            Code = accountSubject.Code,
                            Status = accountSubject.Status,
                            Direction = accountSubject.Direction
                        };
            accountSubjectResponseList.Count = await query.CountAsync();
            accountSubjectResponseList.List = await query.ToListAsync();
            return RickWebResult.Success(accountSubjectResponseList);
        }

        /// <summary>
        /// 新增会计科目
        /// </summary>
        /// <param name="accountSubjectResquest"></param>
        /// <returns></returns>
        [HttpPost]
        [Admin]
        public async Task<object> Post([FromBody] AccountSubjectResquest accountSubjectResquest)
        {
            await _accountsubjectService.BeginTransactionAsync();

            Accountsubject accountsubject = new Accountsubject();
            DateTime now = DateTime.Now;
            accountsubject.Id = _idGenerator.NextId();
            accountsubject.Name = accountSubjectResquest.Name;
            accountsubject.Code = accountSubjectResquest.Code;
            accountsubject.Direction = accountSubjectResquest.Direction;
            accountsubject.Status = 1;
            accountsubject.Addtime = now;
            accountsubject.Lasttime = now;
            accountsubject.Adduser = UserInfo.Id;
            accountsubject.Lastuser = UserInfo.Id;
            await _accountsubjectService.AddAsync(accountsubject);
            await _accountsubjectService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        public class AccountSubjectResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public int Status { get; set; }
            public int Direction { get; set; }
        }
        public class AccountSubjectResponseList
        {
            public int Count { get; set; }
            public List<AccountSubjectResponse> List { get; set; }
        }
        public class AccountSubjectResquest
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public int Direction { get; set; }
        }
    }
}
