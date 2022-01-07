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
    public class RecivedClaimController : RickControllerBase
    {
        private readonly ILogger<RecivedClaimController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public RecivedClaimController(ILogger<RecivedClaimController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询已到库订单
        /// </summary>
        /// <param name="expressnumber"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RecivedClaimResponseList>> Get([FromQuery] string expressnumber, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from expressclaim in _expressclaimService.Query<Expressclaim>(t => (t.Status == (int)ExpressClaimStatus.已揽收 || t.Status == (int)ExpressClaimStatus.已验货) && t.Appuser == UserInfo.Id)
                        join expressinfo in _expressclaimService.Query<Expressinfo>(t => string.IsNullOrEmpty(expressnumber) || t.Expressnumber.Contains(expressnumber))
                        on expressclaim.Expressinfoid equals expressinfo.Id
                        join package in _expressclaimService.Query<Package>(t => t.Status >= 1)
                        on expressclaim.Packageid equals package.Id
                        join courier in _expressclaimService.Query<Courier>(t => 1 == 1)
                        on expressinfo.Courierid equals courier.Id

                        select new RecivedClaimResponse()
                        {
                            Id = expressclaim.Id,
                            PackageName = package.Name,
                            PackageCode = package.Code,
                            Weight = package.Weight,
                            Volume = package.Volume,
                            Expressnumber = expressinfo.Expressnumber,
                            Packageid = package.Id,
                            CourierName = courier.Name,
                            CourierId = expressinfo.Courierid,
                            Addtime = package.Addtime
                        };
            var count = await query.CountAsync();
            var results = await (query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize)).ToListAsync();
            RecivedClaimResponseList recivedClaimResponseList = new RecivedClaimResponseList();

            recivedClaimResponseList.List = results;
            recivedClaimResponseList.Count = count;

            IEnumerable<long> ids = recivedClaimResponseList.List.Select(t => t.Packageid);

            var imageInfos = await (from image in _expressclaimService.Query<Packageimage>(t => ids.Contains(t.Packageid))
                                    select image
                                    ).ToListAsync();

            var vedioInfos = await (from vedio in _expressclaimService.Query<Packagevideo>(t => ids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            var expressclaimids = recivedClaimResponseList.List.Select(t => t.Id);
            var expressclaimdetails = await (from expressclaimdetail in _expressclaimService.Query<Expressclaimdetail>(t => expressclaimids.Contains(t.Expressclaimid))
                                 select new RecivedClaimResponseDetail()
                                 {
                                     Id = expressclaimdetail.Id,
                                     Expressclaimid = expressclaimdetail.Expressclaimid,
                                     Name = expressclaimdetail.Name,
                                     Unitprice = expressclaimdetail.Unitprice,
                                     Count = expressclaimdetail.Count
                                 }).ToListAsync();

            foreach (var packageInResponse in recivedClaimResponseList.List)
            {
                packageInResponse.Images = imageInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Videos = vedioInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Details = expressclaimdetails.Where(t => t.Expressclaimid == packageInResponse.Id).ToList();
            }

            return RickWebResult.Success(recivedClaimResponseList);
        }

        public class RecivedClaimResponse
        {
            public long Id { get; set; }
            public long Packageid { get; set; }
            public string PackageName { get; set; }
            public string PackageCode { get; set; }
            public string Expressnumber { get; set; }
            public string CourierName { get; set; }
            public long? CourierId { get; set; }
            public DateTime Addtime { get; set; }
            public Decimal? Weight { get; set; }
            public Decimal? Volume { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }

            public IList<RecivedClaimResponseDetail> Details { get; set; }
        }
        public class RecivedClaimResponseList
        {
            public int Count { get; set; }
            public IList<RecivedClaimResponse> List { get; set; }
        }
        public class RecivedClaimResponseDetail
        {
            public long Id { get; set; }
            public long Expressclaimid { get; set; }
            public string Name { get; set; }
            public decimal? Unitprice { get; set; }
            public int Count { get; set; }

        }

    }
}
