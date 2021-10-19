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
    public class ChannelController : RickControllerBase
    {
        private readonly ILogger<ChannelController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IChannelService _channelService;
        private readonly RedisClientService _redisClientService;

        public ChannelController(ILogger<ChannelController> logger, IChannelService channelService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _channelService = channelService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取发货渠道
        /// </summary>
        /// <param name="nationId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<ChannelResponse>>> Get([FromQuery] long? nationId)
        {
            var channelDetails = _channelService.Query<Channeldetail>(t => t.Status == 1);
            var channels = _channelService.Query<Channel>(t => t.Status == 1);
            var nations = _channelService.Query<Nation>(t => t.Status == 1 && (!nationId.HasValue || t.Id == nationId));
            IEnumerable<ChannelResponse> result = await (from cd in channelDetails
                         join c in channels
                         on cd.Channelid equals c.Id
                         join n in nations
                         on cd.Nationid equals n.Id
                         select new ChannelResponse()
                         {
                             Id = cd.Id,
                             Nationid = cd.Nationid,
                             NationName = n.Name,
                             Name = c.Name,
                             Unitprice = cd.Unitprice
                         }).ToListAsync();

            return RickWebResult.Success(result);
        }

        public class ChannelRequest
        {

        }
        public class ChannelResponse
        {
            public long Id { get; set; }
            public long Nationid { get; set; }
            public string NationName { get; set; }
            public string Name { get; set; }
            public decimal Unitprice { get; set; }

        }
        public class ChannelResponseList
        {

        }
    }
}
