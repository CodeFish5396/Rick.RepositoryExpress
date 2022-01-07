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
using Rick.RepositoryExpress.SysWebApi.Filters;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepositoryregionController : RickControllerBase
    {
        private readonly ILogger<RepositoryregionController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public RepositoryregionController(ILogger<RepositoryregionController> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询仓库区域
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repositoryid"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositoryregionResponseList>> Get([FromQuery] long? id, [FromQuery] long? repositoryid, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from repositoryregion in _repositoryService.Query<Repositoryregion>()
                        join repository in _repositoryService.Query<Repository>()
                        on repositoryregion.Repositoryid equals repository.Id
                        where (!id.HasValue || repositoryregion.Id == id)
                        && (!status.HasValue || repositoryregion.Status == status)
                        && (string.IsNullOrEmpty(name) || repositoryregion.Name == name)
                        && (!repositoryid.HasValue || repositoryregion.Repositoryid == repositoryid)
                        select new RepositoryregionResponse()
                        {
                            Id = repositoryregion.Id,
                            Name = repositoryregion.Name,
                            Repositoryid = repositoryregion.Repositoryid,
                            Repositoryname = repository.Name,
                            Order = repositoryregion.Order,
                            Addtime = repositoryregion.Addtime
                        };

            RepositoryregionResponseList repositoryResponceList = new RepositoryregionResponseList();
            repositoryResponceList.Count = await query.CountAsync();
            repositoryResponceList.List = await query.OrderBy(t => t.Order).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(repositoryResponceList);
        }

        /// <summary>
        /// 新增仓库区域
        /// </summary>
        /// <param name="repositoryregionPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] RepositoryregionPostRequest repositoryregionPostRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositoryregion repositoryregion = new Repositoryregion();
            repositoryregion.Id = _idGenerator.NextId();
            repositoryregion.Repositoryid = repositoryregionPostRequest.Repositoryid;
            repositoryregion.Name = repositoryregionPostRequest.Name;
            repositoryregion.Order = repositoryregionPostRequest.Order;
            repositoryregion.Status = 1;
            repositoryregion.Adduser = UserInfo.Id;
            repositoryregion.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositoryregion.Addtime = now;
            repositoryregion.Lasttime = now;
            await _repositoryService.AddAsync(repositoryregion);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改仓库
        /// </summary>
        /// <param name="repositoryregionPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] RepositoryregionPutRequest repositoryregionPutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositoryregion repositoryregion = await _repositoryService.FindAsync<Repositoryregion>(t => t.Id == repositoryregionPutRequest.Id);
            repositoryregion.Name = repositoryregionPutRequest.Name;
            repositoryregion.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositoryregion.Lasttime = now;

            await _repositoryService.UpdateAsync(repositoryregion);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除仓库区域
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _repositoryService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            var repositoryregions = await _repositoryService.QueryAsync<Repositoryregion>(t => id == t.Id);
            foreach (var repositoryregion in repositoryregions)
            {
                repositoryregion.Status = 0;
                repositoryregion.Lastuser = UserInfo.Id;
                repositoryregion.Lasttime = now;
                await _repositoryService.UpdateAsync(repositoryregion);
            }
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class RepositoryregionPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }

        }

        public class RepositoryregionPostRequest
        {
            public long Repositoryid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
        }
        public class RepositoryregionResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }

            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class RepositoryregionResponseList
        {
            public int Count { get; set; }
            public IEnumerable<RepositoryregionResponse> List { get; set; }
        }

    }
}
