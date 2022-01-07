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
    public class RepositoryshelfController : RickControllerBase
    {
        private readonly ILogger<RepositoryshelfController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public RepositoryshelfController(ILogger<RepositoryshelfController> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询仓库货架
        /// </summary>
        /// <param name="id"></param>
        /// <param name="regionid"></param>
        /// <param name="repositoryid"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositoryshelfResponseList>> Get([FromQuery] long? id, [FromQuery] long? regionid, [FromQuery] long? repositoryid, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from repositoryshelf in _repositoryService.Query<Repositoryshelf>()
                        join repositoryregion in _repositoryService.Query<Repositoryregion>()
                        on repositoryshelf.Repositoryregionid equals repositoryregion.Id
                        join repository in _repositoryService.Query<Repository>()
                        on repositoryregion.Repositoryid equals repository.Id
                        where (!id.HasValue || repositoryshelf.Id == id)
                        && (!status.HasValue || repositoryshelf.Status == status)
                        && (string.IsNullOrEmpty(name) || repositoryshelf.Name == name)
                        && (!regionid.HasValue || repositoryregion.Id == regionid)
                        && (!repositoryid.HasValue || repository.Id == repositoryid)
                        select new RepositoryshelfResponse()
                        {
                            Id = repositoryshelf.Id,
                            Name = repositoryshelf.Name,
                            Repositoryid = repository.Id,
                            Repositoryname = repository.Name,
                            Repositoryregionid = repositoryregion.Id,
                            Repositoryregionidname = repositoryregion.Name,
                            Order = repositoryshelf.Order,
                            Addtime = repositoryshelf.Addtime
                        };

            RepositoryshelfResponseList repositoryResponceList = new RepositoryshelfResponseList();
            repositoryResponceList.Count = await query.CountAsync();
            repositoryResponceList.List = await query.OrderBy(t => t.Order).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(repositoryResponceList);
        }

        /// <summary>
        /// 新增仓库货架
        /// </summary>
        /// <param name="repositoryregionPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] RepositoryshelfPostRequest repositoryregionPostRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositoryshelf repositoryshelf = new Repositoryshelf();
            repositoryshelf.Id = _idGenerator.NextId();
            repositoryshelf.Repositoryid = repositoryregionPostRequest.Repositoryid;
            repositoryshelf.Repositoryregionid = repositoryregionPostRequest.Repositoryregionid;
            repositoryshelf.Name = repositoryregionPostRequest.Name;
            repositoryshelf.Order = repositoryregionPostRequest.Order;
            repositoryshelf.Status = 1;
            repositoryshelf.Adduser = UserInfo.Id;
            repositoryshelf.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositoryshelf.Addtime = now;
            repositoryshelf.Lasttime = now;
            await _repositoryService.AddAsync(repositoryshelf);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改仓库
        /// </summary>
        /// <param name="repositoryregionPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] RepositoryshelfPutRequest repositoryregionPutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositoryshelf repositoryshelf = await _repositoryService.FindAsync<Repositoryshelf>(t => t.Id == repositoryregionPutRequest.Id);
            repositoryshelf.Name = repositoryregionPutRequest.Name;
            repositoryshelf.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositoryshelf.Lasttime = now;

            await _repositoryService.UpdateAsync(repositoryshelf);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除仓库货架
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _repositoryService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            var repositoryregions = await _repositoryService.QueryAsync<Repositoryshelf>(t => id == t.Id);
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

        public class RepositoryshelfPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }

        }

        public class RepositoryshelfPostRequest
        {
            public long Repositoryid { get; set; }
            public long Repositoryregionid { get; set; }

            public string Name { get; set; }
            public int Order { get; set; }
        }
        public class RepositoryshelfResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }
            public long Repositoryregionid { get; set; }
            public string Repositoryregionidname { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class RepositoryshelfResponseList
        {
            public int Count { get; set; }
            public IEnumerable<RepositoryshelfResponse> List { get; set; }
        }

    }
}
