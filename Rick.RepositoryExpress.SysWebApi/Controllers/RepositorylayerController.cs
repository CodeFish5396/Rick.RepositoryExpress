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
    public class RepositorylayerController : RickControllerBase
    {
        private readonly ILogger<RepositorylayerController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public RepositorylayerController(ILogger<RepositorylayerController> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询仓库层
        /// </summary>
        /// <param name="id"></param>
        /// <param name="shelfid"></param>
        /// <param name="regionid"></param>
        /// <param name="repositoryid"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositorylayerResponseList>> Get([FromQuery] long? id, [FromQuery] long? shelfid, [FromQuery] long? regionid, [FromQuery] long? repositoryid, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from repositorylayer in _repositoryService.Query<Repositorylayer>()
                        join repositoryshelf in _repositoryService.Query<Repositoryshelf>()
                        on repositorylayer.Repositoryshelfid equals repositoryshelf.Id
                        join repositoryregion in _repositoryService.Query<Repositoryregion>()
                        on repositoryshelf.Repositoryregionid equals repositoryregion.Id
                        join repository in _repositoryService.Query<Repository>()
                        on repositoryregion.Repositoryid equals repository.Id
                        where (!id.HasValue || repositorylayer.Id == id)
                        && (!status.HasValue || repositorylayer.Status == status)
                        && (string.IsNullOrEmpty(name) || repositorylayer.Name == name)
                        && (!shelfid.HasValue || repositoryshelf.Id == shelfid)
                        && (!regionid.HasValue || repositoryregion.Id == regionid)
                        && (!repositoryid.HasValue || repository.Id == repositoryid)
                        select new RepositorylayerResponse()
                        {
                            Id = repositorylayer.Id,
                            Name = repositorylayer.Name,
                            Repositoryid = repository.Id,
                            Repositoryname = repository.Name,
                            Repositoryregionid = repositoryregion.Id,
                            Repositoryregionname = repositoryregion.Name,
                            Repositoryshelfid = repositoryshelf.Id,
                            Repositoryshelfname = repositoryshelf.Name,
                            Order = repositorylayer.Order,
                            Addtime = repositorylayer.Addtime
                        };

            RepositorylayerResponseList repositorylayerResponseList = new RepositorylayerResponseList();
            repositorylayerResponseList.Count = await query.CountAsync();
            repositorylayerResponseList.List = await query.OrderBy(t => t.Order).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(repositorylayerResponseList);
        }

        /// <summary>
        /// 新增仓库层
        /// </summary>
        /// <param name="repositorylayerPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] RepositorylayerPostRequest repositorylayerPostRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositorylayer repositorylayer = new Repositorylayer();
            repositorylayer.Id = _idGenerator.NextId();
            repositorylayer.Repositoryid = repositorylayerPostRequest.Repositoryid;
            repositorylayer.Repositoryshelfid = repositorylayerPostRequest.Repositoryshelfid;
            repositorylayer.Name = repositorylayerPostRequest.Name;
            repositorylayer.Order = repositorylayerPostRequest.Order;
            repositorylayer.Status = 1;
            repositorylayer.Adduser = UserInfo.Id;
            repositorylayer.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositorylayer.Addtime = now;
            repositorylayer.Lasttime = now;
            await _repositoryService.AddAsync(repositorylayer);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改仓库
        /// </summary>
        /// <param name="repositorylayerPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] RepositorylayerPutRequest repositorylayerPutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repositorylayer repositorylayer = await _repositoryService.FindAsync<Repositorylayer>(t => t.Id == repositorylayerPutRequest.Id);
            repositorylayer.Name = repositorylayerPutRequest.Name;
            repositorylayer.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repositorylayer.Lasttime = now;

            await _repositoryService.UpdateAsync(repositorylayer);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除仓库层
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _repositoryService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            var repositorylayers = await _repositoryService.QueryAsync<Repositorylayer>(t => id == t.Id);
            foreach (var repositorylayer in repositorylayers)
            {
                repositorylayer.Status = 0;
                repositorylayer.Lastuser = UserInfo.Id;
                repositorylayer.Lasttime = now;
                await _repositoryService.UpdateAsync(repositorylayer);
            }
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class RepositorylayerPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }

        }

        public class RepositorylayerPostRequest
        {
            public long Repositoryid { get; set; }
            public long Repositoryshelfid { get; set; }

            public string Name { get; set; }
            public int Order { get; set; }
        }
        public class RepositorylayerResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }
            public long Repositoryregionid { get; set; }
            public string Repositoryregionname { get; set; }

            public long Repositoryshelfid { get; set; }
            public string Repositoryshelfname { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class RepositorylayerResponseList
        {
            public int Count { get; set; }
            public IEnumerable<RepositorylayerResponse> List { get; set; }
        }

    }
}
