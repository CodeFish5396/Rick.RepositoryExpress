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
    public class GoodtypelController : RickControllerBase
    {
        private readonly ILogger<GoodtypelController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public GoodtypelController(ILogger<GoodtypelController> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询商品类型
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<GoodtypelResponseList>> Get()
        {
            var query = from goodtypel1 in _repositoryService.Query<Goodtypel1>()
                        where goodtypel1.Status == 1
                        select new GoodtypelResponse()
                        {
                            Id = goodtypel1.Id,
                            Name = goodtypel1.Name,
                            Order = goodtypel1.Order,
                            Addtime = goodtypel1.Addtime
                        };

            GoodtypelResponseList goodtypel1ResponseList = new GoodtypelResponseList();
            goodtypel1ResponseList.Count = await query.CountAsync();
            goodtypel1ResponseList.List = await query.OrderBy(t => t.Order).ToListAsync();

            var goodtypel1ids = goodtypel1ResponseList.List.Select(t => t.Id);

            var goodtypel2s = await _repositoryService.QueryAsync<Goodtypel2>(g2 => goodtypel1ids.Contains(g2.Parentid) && g2.Status == 1);
            foreach (GoodtypelResponse goodtypelResponse in goodtypel1ResponseList.List)
            {
                goodtypelResponse.List = goodtypel2s.Where(g2 => g2.Parentid == goodtypelResponse.Id).Select(g2 => new GoodtypelDetailResponse()
                {
                    Id = g2.Id,
                    Parentid = g2.Parentid,
                    Name = g2.Name,
                    Order = g2.Order,
                    Addtime = g2.Addtime
                }).OrderBy(t => t.Order).ToList();
            }

            return RickWebResult.Success(goodtypel1ResponseList);
        }

        public class GoodtypelResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }
            public IList<GoodtypelDetailResponse> List { get; set; }
        }

        public class GoodtypelDetailResponse
        {
            public long Id { get; set; }
            public long Parentid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public DateTime Addtime { get; set; }

        }

        public class GoodtypelResponseList
        {
            public int Count { get; set; }
            public IList<GoodtypelResponse> List { get; set; }
        }

    }
}
