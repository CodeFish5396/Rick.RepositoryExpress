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
    public class ChanneltypeController : RickControllerBase
    {
        private readonly ILogger<ChanneltypeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IChannelService _channelService;
        private readonly RedisClientService _redisClientService;

        public ChanneltypeController(ILogger<ChanneltypeController> logger, IChannelService channelService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _channelService = channelService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        [Admin]
        [HttpGet]
        public async Task<RickWebResult<object>> Get()
        {
            List<string> types = new List<string>();
            types.Add("Led灯具");
            types.Add("内置电池");
            types.Add("内衣");
            types.Add("家居用品");
            types.Add("少儿读物");
            types.Add("布料");
            types.Add("帽子");
            types.Add("服装配饰");
            types.Add("服饰");
            types.Add("衣服");
            types.Add("饰品");
            types.Add("保健用品");
            types.Add("化妆品");
            types.Add("口罩");
            types.Add("品牌/仿牌");
            types.Add("平衡滑板车");
            types.Add("手机");
            types.Add("洗手液");
            types.Add("液体/粉末");
            types.Add("电子烟");
            types.Add("磁性/马达");
            types.Add("移动电源");
            types.Add("纯电池");
            types.Add("膏状化妆品");
            types.Add("配套电池");
            types.Add("防疫物资");
            types.Add("面膜");
            types.Add("食品干货");
            _redisClientService.StringSet("ChannelTypes",string.Join(",", types));

            return RickWebResult.Success(new object());
        }
        
    }
}
