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
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IList<ChannelResponse>>> Get([FromQuery] string name, [FromQuery] int? status)
        {
            var channels = _channelService.Query<Channel>(t => (string.IsNullOrEmpty(name) || t.Name == name) && (!status.HasValue || t.Status == status));
            var temp = await (from channel in channels
                              join channelDetail in _channelService.Query<Channeldetail>(t => t.Status == 1)
                              on channel.Id equals channelDetail.Channelid
                              join nation in _channelService.Query<Nation>(t => 1 == 1)
                              on channelDetail.Nationid equals nation.Id
                              join agent in _channelService.Query<Agent>(t => 1 == 1)
                              on channelDetail.Agentid equals agent.Id
                              select new
                              {
                                  channel.Id,
                                  channel.Name,
                                  channel.Unitprice,
                                  channelDetail.Nationid,
                                  NationName = nation.Name,
                                  channelDetail.Agentid,
                                  AgentName = agent.Name,
                              }).ToListAsync();

            var results = from t in temp
                          group t by new { t.Id, t.Name,t.Unitprice };
            List<ChannelResponse> ChannelResponses = new List<ChannelResponse>();
            foreach (var r in results)
            {
                ChannelResponse channelResponse = new ChannelResponse();
                channelResponse.details = new List<ChannelResponsedetail>();
                channelResponse.Id = r.Key.Id;
                channelResponse.Name = r.Key.Name;
                channelResponse.Unitprice = r.Key.Unitprice;
                foreach (var d in r)
                {
                    ChannelResponsedetail channelResponsedetail = new ChannelResponsedetail();
                    channelResponsedetail.Nationid = d.Nationid;
                    channelResponsedetail.NationName = d.NationName;
                    channelResponsedetail.Agentid = d.Agentid;
                    channelResponsedetail.AgentName = d.AgentName;
                    channelResponse.details.Add(channelResponsedetail);
                }
                ChannelResponses.Add(channelResponse);
            }

            

            return RickWebResult.Success((IList<ChannelResponse>)ChannelResponses);
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
            channel.Unitprice = channelRequest.Unitprice;

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

        /// <summary>
        /// 删除快递通道
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _channelService.BeginTransactionAsync();

            Channel channel = await _channelService.FindAsync<Channel>(t => t.Id == id);
            DateTime now = DateTime.Now;

            channel.Status = 0;
            channel.Lasttime = now;
            channel.Lastuser = UserInfo.Id;
            await _channelService.UpdateAsync(channel);
            await _channelService.CommitAsync();
            return RickWebResult.Success(new object());

        }

        /// <summary>
        /// 修改快递通道
        /// </summary>
        /// <param name="channelRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<ChannelResponse>> Put([FromBody] ChannelPutRequest channelRequest)
        {
            await _channelService.BeginTransactionAsync();

            Channel channelOld = await _channelService.FindAsync<Channel>(t => t.Id == channelRequest.id);
            DateTime now = DateTime.Now;

            channelOld.Status = 0;
            channelOld.Lasttime = now;
            channelOld.Lastuser = UserInfo.Id;
            await _channelService.UpdateAsync(channelOld);

            Channel channel = new Channel();
            channel.Id = _idGenerator.NextId();
            channel.Name = channelRequest.Name;
            channel.Unitprice = channelRequest.Unitprice;
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
        public decimal Unitprice { get; set; }

        public IList<ChannelRequestdetail> details { get; set; }
    }

    public class ChannelRequestdetail
    {
        public long Nationid { get; set; }
        public long Agentid { get; set; }
    }
    public class ChannelPutRequest
    {
        public long id { get; set; }
        public string Name { get; set; }
        public decimal Unitprice { get; set; }
        public IList<ChannelRequestdetail> details { get; set; }
    }

    public class ChannelResponse
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public decimal Unitprice { get; set; }
        public IList<ChannelResponsedetail> details { get; set; }


    }
    public class ChannelResponsedetail
    {
        public long Nationid { get; set; }
        public string NationName { get; set; }

        public long Agentid { get; set; }
        public string AgentName { get; set; }

    }


}
