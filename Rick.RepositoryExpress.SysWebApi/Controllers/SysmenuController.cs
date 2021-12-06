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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SysmenuController : RickControllerBase
    {
        private readonly ILogger<SysmenuController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysmenuService _sysmenuService;
        private readonly RedisClientService _redisClientService;

        public SysmenuController(ILogger<SysmenuController> logger, ISysmenuService sysmenuService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysmenuService = sysmenuService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询菜单树结构
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Admin]
        public async Task<RickWebResult<List<SysmenuTreeResponse>>> Get()
        {
            var menus = await _sysmenuService.QueryAsync<Sysmenu>(t => t.Status == 1);
            List<SysmenuTreeResponse> sysmenuTreeResponses = new List<SysmenuTreeResponse>();
            var fisrtLayerMenus = menus.Where(t => t.Parentid == 0).OrderBy(t => t.Order).Select(t => new SysmenuTreeResponse()
            {
                Id = t.Id,
                Index = t.Index,
                Name = t.Name,
                Parentid = t.Parentid,
                Isdirectory = t.Isdirectory,
                Order = t.Order
            }).ToList();
            sysmenuTreeResponses.AddRange(fisrtLayerMenus);
            foreach (var fisrtLayerMenu in fisrtLayerMenus)
            {
                var secondLayerMenus = menus.Where(t => t.Parentid == fisrtLayerMenu.Id).OrderBy(t => t.Order).Select(t => new SysmenuTreeResponse()
                {
                    Id = t.Id,
                    Index = t.Index,
                    Name = t.Name,
                    Parentid = t.Parentid,
                    Isdirectory = t.Isdirectory,
                    Order = t.Order
                }).ToList();
                fisrtLayerMenu.Subs = secondLayerMenus;
                foreach (var secondLayerMenu in fisrtLayerMenu.Subs)
                {
                    var thirdLayerMenus = menus.Where(t => t.Parentid == secondLayerMenu.Id).OrderBy(t => t.Order).Select(t => new SysmenuTreeResponse()
                    {
                        Id = t.Id,
                        Index = t.Index,
                        Name = t.Name,
                        Parentid = t.Parentid,
                        Isdirectory = t.Isdirectory,
                        Order = t.Order
                    }).ToList();
                    secondLayerMenu.Subs = thirdLayerMenus;
                }
            }
            return RickWebResult.Success(sysmenuTreeResponses);
        }

        /// <summary>
        /// 添加菜单
        /// </summary>
        /// <param name="sysmenuRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [Admin]
        public async Task<RickWebResult<object>> Post([FromBody] SysmenuRequest sysmenuRequest)
        {
            await _sysmenuService.BeginTransactionAsync();

            Sysmenu sysmenu = new Sysmenu();
            DateTime now = DateTime.Now;
            sysmenu.Id = _idGenerator.NextId();

            sysmenu.Index = sysmenuRequest.Index;
            sysmenu.Name = sysmenuRequest.Name;
            sysmenu.Parentid = sysmenuRequest.Parentid;
            sysmenu.Isdirectory = sysmenuRequest.Isdirectory;
            sysmenu.Order = sysmenuRequest.Order;

            sysmenu.Status = 1;
            sysmenu.Addtime = now;
            sysmenu.Lasttime = now;
            sysmenu.Adduser = UserInfo.Id;
            sysmenu.Lastuser = UserInfo.Id;
            await _sysmenuService.AddAsync(sysmenu);
            await _sysmenuService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class SysmenuRequest
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public long? Parentid { get; set; }
            public sbyte Isdirectory { get; set; }
            public int Order { get; set; }
        }

        public class SysmenuTreeResponse
        {
            public long Id { get; set; }
            public string Index { get; set; }
            public string Name { get; set; }
            public long? Parentid { get; set; }
            public sbyte Isdirectory { get; set; }
            public int Order { get; set; }
            public List<SysmenuTreeResponse> Subs { get; set; }
        }

    }
}
