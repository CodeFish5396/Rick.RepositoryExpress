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
    /// 系统设置
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SyssettingController : RickControllerBase
    {
        private readonly ILogger<SyssettingController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISyssettingService _syssettingService;
        private readonly RedisClientService _redisClientService;

        public SyssettingController(ILogger<SyssettingController> logger, ISyssettingService syssettingService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _syssettingService = syssettingService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询系统设置
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<SyssettingResponseList>> Get([FromQuery] string name, [FromQuery] string code, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            int count = await _syssettingService.CountAsync<Syssetting>(t => (string.IsNullOrEmpty(name) || t.Name == name) && (string.IsNullOrEmpty(code) || t.Code == code));

            var results = _syssettingService.Query<Syssetting>(t => (string.IsNullOrEmpty(name) || t.Name == name) && (string.IsNullOrEmpty(code) || t.Code == code))
                .OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize);

            SyssettingResponseList syssettingResponseList = new SyssettingResponseList();
            syssettingResponseList.Count = count;

            syssettingResponseList.List = results.Select(t => new SyssettingResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Value = t.Value
            }).ToList();
            return RickWebResult.Success(syssettingResponseList);
        }

        /// <summary>
        /// 创建系统设置
        /// </summary>
        /// <param name="syssettingRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] SyssettingRequest syssettingRequest)
        {
            await _syssettingService.BeginTransactionAsync();

            Syssetting syssetting = new Syssetting();
            DateTime now = DateTime.Now;
            syssetting.Id = _idGenerator.NextId();
            syssetting.Name = syssettingRequest.Name;
            syssetting.Code = syssettingRequest.Code;
            syssetting.Value = syssettingRequest.Value;
            syssetting.Addtime = now;
            syssetting.Lasttime = now;
            syssetting.Adduser = UserInfo.Id;
            syssetting.Lastuser = UserInfo.Id;
            await _syssettingService.AddAsync(syssetting);
            await _syssettingService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        /// <summary>
        /// 修改系统设置
        /// </summary>
        /// <param name="syssettingPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] SyssettingPutRequest syssettingPutRequest)
        {
            await _syssettingService.BeginTransactionAsync();

            var syssetting = await _syssettingService.FindAsync<Syssetting>(syssettingPutRequest.Id);
            DateTime now = DateTime.Now;
            syssetting.Name = syssettingPutRequest.Name;
            syssetting.Value = syssettingPutRequest.Value;
            syssetting.Lasttime = now;
            syssetting.Lastuser = UserInfo.Id;
            await _syssettingService.UpdateAsync(syssetting);
            await _syssettingService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除系统设置
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public class SyssettingRequest
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Value { get; set; }
        }
        public class SyssettingPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class SyssettingResponse
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }

        }
        public class SyssettingResponseList
        {
            public int Count { get; set; }
            public List<SyssettingResponse> List { get; set; }
        }
    }



}
