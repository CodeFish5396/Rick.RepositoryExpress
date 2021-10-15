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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 快递通道
    /// </summary>
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
        /// 查询快递通道
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<ChannelResponse>>> Get()
        {
            var results = await _channelService.QueryAsync<Channel>(t => t.Status == 1);
            return RickWebResult.Success(results.Select(t => new ChannelResponse()
            {
                Id = t.Id,
                Name = t.Name
            }));
        }

        /// <summary>
        /// 创建快递通道
        /// </summary>
        /// <param name="channelRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<ChannelResponse>> Post([FromBody] ChannelRequest channelRequest)
        {
            await _channelService.BeginTransactionAsync();

            Channel channel = new Channel();
            DateTime now = DateTime.Now;
            channel.Id = _idGenerator.NextId();
            channel.Name = channelRequest.Name;
            channel.Status = 1;
            channel.Addtime = now;
            channel.Lasttime = now;
            channel.Adduser = UserInfo.Id;
            channel.Lastuser = UserInfo.Id;
            await _channelService.AddAsync(channel);

            foreach (var detail in channelRequest.details)
            {
                Channeldetail channeldetail = new Channeldetail();
                channeldetail.Id = _idGenerator.NextId();
                channeldetail.Channelid = channel.Id;
                channeldetail.Nationid = detail.Nationid;
                channeldetail.Agentid = detail.Agentid;
                channeldetail.Unitprice = detail.Unitprice;
                channeldetail.Status = 1;
                channeldetail.Addtime = now;
                channeldetail.Lasttime = now;
                channeldetail.Adduser = UserInfo.Id;
                channeldetail.Lastuser = UserInfo.Id;
                await _channelService.AddAsync(channeldetail);
            }

            await _channelService.CommitAsync();
            ChannelResponse channelResponse = new ChannelResponse();
            channelResponse.Id = channel.Id;
            channelResponse.Name = channel.Name;
            return RickWebResult.Success(channelResponse);
        }

    }

    public class ChannelRequest
    {
        public string Name { get; set; }
        public IList<ChannelRequestdetail> details { get; set; }
    }
    public class ChannelRequestdetail
    {
        public long Nationid { get; set; }
        public long Agentid { get; set; }
        public decimal Unitprice { get; set; }
    }

    public class ChannelResponse
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public IList<ChannelResponsedetail> details { get; set; }


    }
    public class ChannelResponsedetail
    {
        public long Id { get; set; }
        public long Channelid { get; set; }
        public long Nationid { get; set; }
        public string NationName { get; set; }

        public long Agentid { get; set; }
        public string AgentName { get; set; }

        public decimal Unitprice { get; set; }
    }

}
