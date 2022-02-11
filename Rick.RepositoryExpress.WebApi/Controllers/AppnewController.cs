using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppnewController : RickControllerBase
    {
        private readonly ILogger<AppnewController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppnewService _appnewService;
        private readonly RedisClientService _redisClientService;

        public AppnewController(ILogger<AppnewController> logger, IAppnewService appnewService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appnewService = appnewService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }
        [HttpGet]
        public async Task<RickWebResult<AppnewGetResponseList>> Get([FromQuery] int? type, [FromQuery] int? isshow, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from appnew in _appnewService.Query<Appnew>(t => t.Status == 1 && (!type.HasValue || t.Type == type) && (!isshow.HasValue || t.Isshow == isshow))
                        select new AppnewGetResponse()
                        {
                            Id = appnew.Id,
                            Title = appnew.Title,
                            Type = appnew.Type,
                            Vicetitle = appnew.Vicetitle,
                            Imageid = appnew.Imageid,
                            Urlid = appnew.Urlid,
                            Status = appnew.Status,
                            Addtime = appnew.Addtime
                        };
            AppnewGetResponseList appnewGetResponseList = new AppnewGetResponseList();
            appnewGetResponseList.Count = await query.CountAsync();

            appnewGetResponseList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(appnewGetResponseList);
        }
        public class AppnewGetResponseList
        {
            public int Count { get; set; }
            public List<AppnewGetResponse> List { get; set; }

        }
        public class AppnewGetResponse
        {
            public long Id { get; set; }

            public string Title { get; set; }
            public int Type { get; set; }
            public string Vicetitle { get; set; }
            public long Imageid { get; set; }
            public long Urlid { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
        }
    }
}
