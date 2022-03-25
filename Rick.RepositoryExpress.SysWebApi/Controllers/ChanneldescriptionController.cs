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
using Newtonsoft.Json.Converters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChanneldescriptionController : RickControllerBase
    {
        private readonly ILogger<ChanneldescriptionController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IChannelService _channelService;
        private readonly RedisClientService _redisClientService;

        public ChanneldescriptionController(ILogger<ChanneldescriptionController> logger, IChannelService channelService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _channelService = channelService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 添加渠道描述
        /// </summary>
        /// <param name="channeldescriptionPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] ChanneldescriptionPostRequest channeldescriptionPostRequest)
        {
            await _channelService.BeginTransactionAsync();

            Channel channel = await _channelService.FindAsync<Channel>(channeldescriptionPostRequest.Id);
            var oldChanneldescriptions = await _channelService.QueryAsync<Channeldescription>(cd => cd.Channelid == channel.Id);

            foreach (var oldc in oldChanneldescriptions)
            {
                await _channelService.DeleteAsync<Channeldescription>(oldc.Id);
            }

            var oldChannelLimits = await _channelService.QueryAsync<Channellimit>(cd => cd.Channelid == channel.Id);

            foreach (var oldL in oldChannelLimits)
            {
                await _channelService.DeleteAsync<Channellimit>(oldL.Id);
            }

            var oldChanneltypes = await _channelService.QueryAsync<Channeltype>(cd => cd.Channelid == channel.Id);
            foreach (var oldT in oldChanneltypes)
            {
                await _channelService.DeleteAsync<Channeltype>(oldT.Id);
            }

            foreach (ChanneldescriptionPostDetailRequest channeldescriptionPostDetailRequest in channeldescriptionPostRequest.Descriptions)
            {
                Channeldescription channeldescription = new Channeldescription();
                channeldescription.Id = _idGenerator.NextId();
                channeldescription.Channelid = channel.Id;
                channeldescription.Description = channeldescriptionPostDetailRequest.Description;
                channeldescription.Order = channeldescriptionPostDetailRequest.Order;
                await _channelService.AddAsync(channeldescription);
            }

            foreach (ChannellimitPostRequest channellimitPostRequest in channeldescriptionPostRequest.Limits)
            {
                Channellimit channellimit = new Channellimit();
                channellimit.Id = _idGenerator.NextId();
                channellimit.Channelid = channel.Id;
                channellimit.Name = channellimitPostRequest.Name;
                channellimit.Value = channellimitPostRequest.Value;
                channellimit.Order = channellimitPostRequest.Order;

                await _channelService.AddAsync(channellimit);
            }


            foreach (ChanneltypePostRequest channeltypePostRequest in channeldescriptionPostRequest.Types)
            {
                if (channeltypePostRequest.Ischecked)
                {
                    Channeltype channeltype = new Channeltype();
                    channeltype.Id = _idGenerator.NextId();
                    channeltype.Channelid = channel.Id;
                    channeltype.Name = channeltypePostRequest.Name;

                    await _channelService.AddAsync(channeltype);
                }
            }
            var oldChannelWorkday = await _channelService.QueryAsync<Channelworkday>(cw => cw.Channelid == channel.Id);
            foreach (var olW in oldChannelWorkday)
            {
                await _channelService.DeleteAsync<Channelworkday>(olW.Id);
            }
            {
                Channelworkday channelworkday = new Channelworkday();
                channelworkday.Id = _idGenerator.NextId();
                channelworkday.Channelid = channel.Id;
                channelworkday.Workday = channeldescriptionPostRequest.Workday;
                await _channelService.AddAsync(channelworkday);
            }

            await _channelService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 获取渠道描述
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<ChanneldescriptionGetResponse>> Get([FromQuery] long id)
        {
            Channel channel = await _channelService.FindAsync<Channel>(id);
            ChanneldescriptionGetResponse channeldescriptionGetResponse = new ChanneldescriptionGetResponse();
            channeldescriptionGetResponse.Id = channel.Id;


            var channeldescriptions = await _channelService.QueryAsync<Channeldescription>(cd => cd.Channelid == channel.Id);
            var channelLimits = await _channelService.QueryAsync<Channellimit>(cd => cd.Channelid == channel.Id);
            var Channeltypes = await _channelService.QueryAsync<Channeltype>(cd => cd.Channelid == channel.Id);
            var channelDays = await _channelService.QueryAsync<Channelworkday>(cw => cw.Channelid == id);

            channeldescriptionGetResponse.Descriptions = channeldescriptions.Select(t => new ChanneldescriptionDetailGetResponse()
            {
                Description = t.Description,
                Order = t.Order
            }).OrderBy(t => t.Order).ToList();
            channeldescriptionGetResponse.Limits = channelLimits.Select(t => new ChannellimitGetResponse()
            {
                Name = t.Name,
                Value = t.Value,
                Order = t.Order
            }).OrderBy(t => t.Order).ToList();

            var channelTypes = _redisClientService.StringGet("ChannelTypes").Split(",");

            channeldescriptionGetResponse.Types = channelTypes.Select(ctype => new ChanneltypeGetRequest()
            {
                Name = ctype,
                Ischecked = Channeltypes.Any(t=>t.Name == ctype)
            }).ToList();
            channeldescriptionGetResponse.Workday = channelDays.SingleOrDefault()?.Workday;

            return RickWebResult.Success(channeldescriptionGetResponse);
        }

        public class ChanneldescriptionGetResponse
        {
            public long Id { get; set; }
            public string Workday { get; set; }
            public List<ChanneldescriptionDetailGetResponse> Descriptions { get; set; }
            public List<ChannellimitGetResponse> Limits { get; set; }
            public List<ChanneltypeGetRequest> Types { get; set; }

        }
        public class ChanneldescriptionDetailGetResponse
        {
            public string Description { get; set; }
            public int Order { get; set; }
        }
        public class ChannellimitGetResponse
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int Order { get; set; }
        }
        public class ChanneltypeGetRequest
        {
            public string Name { get; set; }
            public bool Ischecked { get; set; }
        }


        public class ChanneldescriptionPostRequest
        {
            public long Id { get; set; }
            public string Workday { get; set; }
            public List<ChanneldescriptionPostDetailRequest> Descriptions { get; set; }
            public List<ChannellimitPostRequest> Limits { get; set; }
            public List<ChanneltypePostRequest> Types { get; set; }
        }

        public class ChanneldescriptionPostDetailRequest
        {
            public string Description { get; set; }
            public int Order { get; set; }
        }

        public class ChannellimitPostRequest
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int Order { get; set; }
        }
        public class ChanneltypePostRequest
        {
            public string Name { get; set; }
            public bool Ischecked { get; set; }
        }

    }
}
