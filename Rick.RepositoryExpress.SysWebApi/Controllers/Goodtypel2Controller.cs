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
    public class Goodtypel2Controller : RickControllerBase
    {
        private readonly ILogger<Goodtypel2Controller> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public Goodtypel2Controller(ILogger<Goodtypel2Controller> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
        public async Task<RickWebResult<Goodtypel2ResponseList>> Get([FromQuery] long? id, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from goodtypel2 in _repositoryService.Query<Goodtypel2>()
                        join goodtypel1 in _repositoryService.Query<Goodtypel1>()
                        on goodtypel2.Parentid equals goodtypel1.Id
                        where (!id.HasValue || goodtypel2.Id == id)
                        && (!status.HasValue || goodtypel2.Status == status)
                        && (string.IsNullOrEmpty(name) || goodtypel2.Name == name)
                        select new Goodtypel2Response()
                        {
                            Id = goodtypel2.Id,
                            Name = goodtypel2.Name,
                            Parentid = goodtypel1.Id,
                            Parentname = goodtypel1.Name,
                            Order = goodtypel2.Order,
                            Addtime = goodtypel2.Addtime,
                            Code = goodtypel2.Code
                        };

            Goodtypel2ResponseList goodtypel2ResponseList = new Goodtypel2ResponseList();
            goodtypel2ResponseList.Count = await query.CountAsync();
            goodtypel2ResponseList.List = await query.OrderBy(t => t.Order).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(goodtypel2ResponseList);
        }

        /// <summary>
        /// 新增商品类型2级
        /// </summary>
        /// <param name="goodtypel2PostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] Goodtypel2PostRequest goodtypel2PostRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Goodtypel2 goodtypel2 = new Goodtypel2();
            goodtypel2.Id = _idGenerator.NextId();
            goodtypel2.Name = goodtypel2PostRequest.Name;
            goodtypel2.Order = goodtypel2PostRequest.Order;
            goodtypel2.Code = goodtypel2PostRequest.Code;
            goodtypel2.Parentid = goodtypel2PostRequest.Parentid;
            goodtypel2.Purpose = goodtypel2PostRequest.Purpose;
            goodtypel2.Unitvalue = goodtypel2PostRequest.Unitvalue;
            goodtypel2.Totalvalue = goodtypel2PostRequest.Totalvalue;
            goodtypel2.Status = 1;
            goodtypel2.Adduser = UserInfo.Id;
            goodtypel2.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            goodtypel2.Addtime = now;
            goodtypel2.Lasttime = now;
            await _repositoryService.AddAsync(goodtypel2);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改商品类型1级
        /// </summary>
        /// <param name="goodtypel2PutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] Goodtypel2PutRequest goodtypel2PutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Goodtypel2 goodtypel2 = await _repositoryService.FindAsync<Goodtypel2>(t => t.Id == goodtypel2PutRequest.Id);
            goodtypel2.Name = goodtypel2PutRequest.Name;
            goodtypel2.Code = goodtypel2PutRequest.Code;
            goodtypel2.Purpose = goodtypel2PutRequest.Purpose;
            goodtypel2.Unitvalue = goodtypel2PutRequest.Unitvalue;
            goodtypel2.Totalvalue = goodtypel2PutRequest.Totalvalue;
            goodtypel2.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            goodtypel2.Lasttime = now;

            await _repositoryService.UpdateAsync(goodtypel2);
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
            Goodtypel2 goodtypel2 = await _repositoryService.FindAsync<Goodtypel2>(id);
            goodtypel2.Status = 0;
            goodtypel2.Lastuser = UserInfo.Id;
            goodtypel2.Lasttime = now;
            await _repositoryService.UpdateAsync(goodtypel2);
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class Goodtypel2PutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public string Code { get; set; }
            public string Purpose { get; set; }
            public string Unitvalue { get; set; }
            public string Totalvalue { get; set; }
        }

        public class Goodtypel2PostRequest
        {
            public long Parentid { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public int Order { get; set; }
            public string Purpose { get; set; }
            public string Unitvalue { get; set; }
            public string Totalvalue { get; set; }
        }
        public class Goodtypel2Response
        {
            public long Id { get; set; }
            public long Parentid { get; set; }
            public string Parentname { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }
        public class Goodtypel2ResponseList
        {
            public int Count { get; set; }
            public IEnumerable<Goodtypel2Response> List { get; set; }
        }

    }
}
