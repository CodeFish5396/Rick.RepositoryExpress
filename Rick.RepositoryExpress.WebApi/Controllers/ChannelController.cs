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
        public async Task<RickWebResult<List<ChannelResponse>>> Get([FromQuery] long? nationId)
        {
            var channelDetails = _channelService.Query<Channeldetail>(t => t.Status == 1);
            var channels = _channelService.Query<Channel>(t => t.Status == 1);
            var nations = _channelService.Query<Nation>(t => t.Status == 1 && (!nationId.HasValue || t.Id == nationId));
            List<ChannelResponse> result = await (from c in channels
                                                  join cd in channelDetails
                                                  on c.Id equals cd.Channelid
                                                  join n in nations
                                                  on cd.Nationid equals n.Id
                                                  select new ChannelResponse()
                                                  {
                                                      Id = c.Id,
                                                      Name = c.Name,
                                                      Unitprice = c.Unitprice
                                                  }).Distinct().ToListAsync();

            var chaneelIds = result.Select(t => t.Id).ToList();
            var channelPrices = await _channelService.QueryAsync<Channelprice>(t => t.Status == 1 && chaneelIds.Contains(t.Channelid));

            var channeldescriptions = await _channelService.QueryAsync<Channeldescription>(cd => chaneelIds.Contains(cd.Channelid));
            var channelLimits = await _channelService.QueryAsync<Channellimit>(cd => chaneelIds.Contains(cd.Channelid));
            var Channeltypes = await _channelService.QueryAsync<Channeltype>(cd => chaneelIds.Contains(cd.Channelid));

            var channelTypes = _redisClientService.StringGet("ChannelTypes").Split(",");

            foreach (ChannelResponse channelResponse in result)
            {
                channelResponse.Pricedetails = (from channelPrice in channelPrices
                                                join nation in nations
                                                on channelPrice.Nationid equals nation.Id
                                                where channelPrice.Channelid == channelResponse.Id
                                                select new ChannelResponsepricedetail()
                                                {
                                                    Id = channelPrice.Id,
                                                    Channelid = channelPrice.Channelid,
                                                    Nationid = channelPrice.Nationid,
                                                    Nationname = nation.Name,
                                                    Minweight = channelPrice.Minweight,
                                                    Maxweight = channelPrice.Maxweight,
                                                    Price = channelPrice.Price
                                                }).ToList();

                channelResponse.Descriptions = channeldescriptions.Where(t => t.Channelid == channelResponse.Id).Select(t => new ChanneldescriptionResponse()
                {
                    Description = t.Description,
                    Order = t.Order
                }).OrderBy(t => t.Order).ToList();

                channelResponse.Limits = channelLimits.Where(t => t.Channelid == channelResponse.Id).Select(t => new ChannellimitResponse()
                {
                    Name = t.Name,
                    Value = t.Value,
                    Order = t.Order
                }).OrderBy(t => t.Order).ToList();

                channelResponse.Types = channelTypes.Select(ctype => new ChanneltypeResponse()
                {
                    Name = ctype,
                    Ischecked = Channeltypes.Any(t => t.Name == ctype && t.Channelid == channelResponse.Id)
                }).ToList();


            }

            return RickWebResult.Success(result);
        }

        public class ChannelRequest
        {

        }

        public class ChannelResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public decimal Unitprice { get; set; }
            public List<ChannelResponsepricedetail> Pricedetails { get; set; }
            public List<ChanneldescriptionResponse> Descriptions { get; set; }
            public List<ChannellimitResponse> Limits { get; set; }
            public List<ChanneltypeResponse> Types { get; set; }


        }
        public class ChannelResponseList
        {

        }
        public class ChannelResponsepricedetail
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public decimal Minweight { get; set; }
            public decimal Maxweight { get; set; }
            public decimal Price { get; set; }

        }

        public class ChanneldescriptionResponse
        {
            public string Description { get; set; }
            public int Order { get; set; }
        }

        public class ChannellimitResponse
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int Order { get; set; }
        }
        public class ChanneltypeResponse
        {
            public string Name { get; set; }
            public bool Ischecked { get; set; }
        }
    }
}
