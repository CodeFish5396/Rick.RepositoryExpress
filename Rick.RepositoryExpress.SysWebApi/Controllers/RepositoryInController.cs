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
    public class RepositoryInController : RickControllerBase
    {
        private readonly ILogger<RepositoryInController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public RepositoryInController(ILogger<RepositoryInController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询包裹入库
        /// </summary>
        /// <param name="expressNumber"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositoryInResponseList>> Get([FromQuery] string expressNumber, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            int count = await _packageService.CountAsync<Package>(t => (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                          && (startTime != DateTime.MinValue || t.Addtime >= startTime)
                          && (endTime != DateTime.MinValue || t.Addtime <= endTime));

            var results = _packageService.Query<Package>(t => (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                          && (startTime != DateTime.MinValue || t.Addtime >= startTime)
                          && (endTime != DateTime.MinValue || t.Addtime <= endTime))
                .OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize);
            RepositoryInResponseList repositoryInResponceList = new RepositoryInResponseList();
            repositoryInResponceList.Count = count;

            repositoryInResponceList.List = await (from package in results
                                                   join user in _packageService.Query<Sysuser>(t => true)
                                                   on package.Lastuser equals user.Id
                                                   select new RepositoryInResponse()
                                                   {
                                                       Id = package.Id,
                                                       Expressnumber = package.Expressnumber,
                                                       Code = package.Code,
                                                       CourierId = package.Courierid,
                                                       Name = package.Name,
                                                       Weight = package.Weight,
                                                       Count = package.Count,
                                                       Lastuser = package.Lastuser,
                                                       Lastusername = user.Name,
                                                       Location = package.Location
                                                   }).ToListAsync();

            return RickWebResult.Success(repositoryInResponceList);
        }

        /// <summary>
        /// 包裹入库录单
        /// </summary>
        /// <param name="repositoryInRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<RepositoryInResponse>> Post([FromBody] RepositoryInRequest repositoryInRequest)
        {
            await _packageService.BeginTransactionAsync();
            Package package = await _packageService.FindAsync<Package>(repositoryInRequest.Id);
            package.Location = repositoryInRequest.Location;
            DateTime now = DateTime.Now;
            package.Lasttime = now;
            package.Lastuser = UserInfo.Id;

            await _packageService.UpdateAsync(package);

            Packagenote packagenote = new Packagenote();
            packagenote.Id = _idGenerator.NextId();
            packagenote.Packageid = package.Id;
            packagenote.Status = 1;
            packagenote.Adduser = UserInfo.Id;
            packagenote.Addtime = now;
            packagenote.Isclosed = 0;
            packagenote.Operator = 2;
            packagenote.Operatoruser = UserInfo.Id;
            await _packageService.AddAsync(packagenote);

            await _packageService.CommitAsync();

            RepositoryInResponse repositoryInResponce = new RepositoryInResponse() {
                Id = package.Id,
                Expressnumber = package.Expressnumber,
                Code = package.Code,
                CourierId = package.Courierid,
                Name = package.Name,
                Weight = package.Weight,
                Count = package.Count,
                Lastuser = package.Lastuser,
                Lastusername = UserInfo.Name,
                Location = package.Location
            };

            return RickWebResult.Success(repositoryInResponce);
        }

        public class RepositoryInRequest
        {
            public long Id { get; set; }
            public string Location { get; set; }
        }

        public class RepositoryInResponse
        {
            public long Id { get; set; }
            public string Expressnumber { get; set; }
            public string Code { get; set; }
            public long CourierId { get; set; }
            public string CourierName { get; set; }
            public string Name { get; set; }
            public decimal Weight { get; set; }
            public int Count { get; set; }
            public long Lastuser { get; set; }
            public string Lastusername { get; set; }
            public string Location { get; set; }

        }
        public class RepositoryInResponseList
        {
            public int Count { get; set; }
            public IList<RepositoryInResponse> List { get; set; }
        }
    }
}
