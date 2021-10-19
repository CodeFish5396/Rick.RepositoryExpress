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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 国家
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NationController : RickControllerBase
    {
        private readonly ILogger<NationController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly INationService _nationService;
        private readonly RedisClientService _redisClientService;

        public NationController(ILogger<NationController> logger, INationService nationService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _nationService = nationService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询国家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<NationResponseList>> Get([FromQuery] long? id, [FromQuery] string name, [FromQuery] string code, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            int count = await _nationService.CountAsync<Nation>(t => (!id.HasValue || t.Id == id) && (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name) && (string.IsNullOrEmpty(code) || t.Code == code));

            var results = _nationService.Query<Nation>(t => (!id.HasValue || t.Id == id) && (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name) && (string.IsNullOrEmpty(code) || t.Code == code))
                .OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize);
            NationResponseList nationResponseList = new NationResponseList();
            nationResponseList.Count = count;
            nationResponseList.List = results.Select(t => new NationResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name
            });
            return RickWebResult.Success(nationResponseList);
        }

        /// <summary>
        /// 创建国家
        /// </summary>
        /// <param name="nationRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<NationResponse>> Post([FromBody] NationRequest nationRequest)
        {
            await _nationService.BeginTransactionAsync();

            Nation nation = new Nation();
            DateTime now = DateTime.Now;
            nation.Id = _idGenerator.NextId();
            nation.Name = nationRequest.Name;
            nation.Code = nationRequest.Code;
            nation.Status = 1;
            nation.Addtime = now;
            nation.Lasttime = now;
            nation.Adduser = UserInfo.Id;
            nation.Lastuser = UserInfo.Id;
            await _nationService.AddAsync(nation);
            await _nationService.CommitAsync();
            NationResponse nationResponse = new NationResponse();
            nationResponse.Id = nation.Id;
            nationResponse.Name = nation.Name;
            nationResponse.Code = nation.Code;
            return RickWebResult.Success(nationResponse);

        }

        /// <summary>
        /// 修改国家
        /// </summary>
        /// <param name="nationPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<NationResponse>> Put([FromBody] NationPutRequest nationPutRequest)
        {
            await _nationService.BeginTransactionAsync();

            var nation = await _nationService.FindAsync<Nation>(nationPutRequest.Id);
            DateTime now = DateTime.Now;
            nation.Name = nationPutRequest.Name;
            nation.Code = nationPutRequest.Code;
            nation.Status = 1;
            nation.Lasttime = now;
            nation.Lastuser = UserInfo.Id;
            await _nationService.UpdateAsync(nation);
            await _nationService.CommitAsync();
            NationResponse nationResponse = new NationResponse();
            nationResponse.Id = nation.Id;
            nationResponse.Name = nation.Name;
            nationResponse.Code = nation.Code;
            return RickWebResult.Success(nationResponse);
        }

        /// <summary>
        /// 删除国家
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<NationResponse>> Delete([FromQuery] long id)
        {
            await _nationService.BeginTransactionAsync();
            Nation nation = await _nationService.FindAsync<Nation>(id);
            DateTime now = DateTime.Now;
            nation.Status = 0;
            nation.Lasttime = now;
            nation.Lastuser = UserInfo.Id;

            await _nationService.UpdateAsync(nation);
            await _nationService.CommitAsync();
            NationResponse nationResponse = new NationResponse();
            nationResponse.Id = nation.Id;
            nationResponse.Code = nation.Code;
            nationResponse.Name = nation.Name;
            return RickWebResult.Success(nationResponse);
        }

    }

    public class NationRequest
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
    public class NationPutRequest
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class NationResponse
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
    public class NationResponseList
    {
        public int Count { get; set; }
        public IEnumerable<NationResponse> List { get; set; }
    }

}
