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
    public class Goodtypel1Controller : RickControllerBase
    {
        private readonly ILogger<Goodtypel1Controller> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public Goodtypel1Controller(ILogger<Goodtypel1Controller> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询商品类型1级
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<Goodtypel1ResponseList>> Get([FromQuery] long? id, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from goodtypel1 in _repositoryService.Query<Goodtypel1>()
                        where (!id.HasValue || goodtypel1.Id == id)
                        && (!status.HasValue || goodtypel1.Status == status)
                        && (string.IsNullOrEmpty(name) || goodtypel1.Name == name)
                        select new Goodtypel1Response()
                        {
                            Id = goodtypel1.Id,
                            Name = goodtypel1.Name,
                            Order = goodtypel1.Order,
                            Addtime = goodtypel1.Addtime
                        };

            Goodtypel1ResponseList goodtypel1ResponseList = new Goodtypel1ResponseList();
            goodtypel1ResponseList.Count = await query.CountAsync();
            goodtypel1ResponseList.List = await query.OrderBy(t => t.Order).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(goodtypel1ResponseList);
        }

        /// <summary>
        /// 新增商品类型1级
        /// </summary>
        /// <param name="goodtypel1PostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] Goodtypel1PostRequest goodtypel1PostRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Goodtypel1 goodtypel1 = new Goodtypel1();
            goodtypel1.Id = _idGenerator.NextId();
            goodtypel1.Name = goodtypel1PostRequest.Name;
            goodtypel1.Order = goodtypel1PostRequest.Order;
            goodtypel1.Status = 1;
            goodtypel1.Adduser = UserInfo.Id;
            goodtypel1.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            goodtypel1.Addtime = now;
            goodtypel1.Lasttime = now;
            await _repositoryService.AddAsync(goodtypel1);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改商品类型1级
        /// </summary>
        /// <param name="goodtypel1PutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] Goodtypel1PutRequest goodtypel1PutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Goodtypel1 goodtypel1 = await _repositoryService.FindAsync<Goodtypel1>(t => t.Id == goodtypel1PutRequest.Id);
            goodtypel1.Name = goodtypel1PutRequest.Name;
            goodtypel1.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            goodtypel1.Lasttime = now;

            await _repositoryService.UpdateAsync(goodtypel1);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除商品类型1级
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _repositoryService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            Goodtypel1 goodtypel1 = await _repositoryService.FindAsync<Goodtypel1>(id);
            goodtypel1.Status = 0;
            goodtypel1.Lastuser = UserInfo.Id;
            goodtypel1.Lasttime = now;
            await _repositoryService.UpdateAsync(goodtypel1);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class Goodtypel1PutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }

        }

        public class Goodtypel1PostRequest
        {
            public string Name { get; set; }
            public int Order { get; set; }
        }
        public class Goodtypel1Response
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class Goodtypel1ResponseList
        {
            public int Count { get; set; }
            public IEnumerable<Goodtypel1Response> List { get; set; }
        }

    }
}
