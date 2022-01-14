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
    public class UnclaimPackageController : RickControllerBase
    {
        private readonly ILogger<UnclaimPackageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public UnclaimPackageController(ILogger<UnclaimPackageController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询已到库但未关联预报订单
        /// </summary>
        /// <param name="expressnumber"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UnclaimPackageResponseList>> Get([FromQuery] string expressnumber, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var packageIds = from expressclaim in _expressclaimService.Query<Expressclaim>(t => t.Packageid.HasValue && t.Packageid > 0 && t.Status > 0)
                             select (long)expressclaim.Packageid;

            var query = from package in _expressclaimService.Query<Package>(t => t.Status >= 1)
                        join courier in _expressclaimService.Query<Courier>(t => 1 == 1)
                        on package.Courierid equals courier.Id
                        where !packageIds.Contains(package.Id)
                        && package.Claimtype == 0
                        select new UnclaimPackageResponse()
                        {
                            Id = package.Id,
                            Expressnumber = package.Expressnumber,
                            CourierName = courier.Name,
                            CourierId = package.Courierid,
                            Addtime = package.Addtime
                        };
            var count = await query.CountAsync();
            var results = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).Select(t => new UnclaimPackageResponse()
            {
                Id = t.Id,
                Expressnumber = t.Expressnumber.Substring(0,t.Expressnumber.Length - 4) + "****",
                CourierName = t.CourierName,
                CourierId = t.CourierId,
                Addtime = t.Addtime
            }).ToListAsync();

            UnclaimPackageResponseList unclaimPackageResponseList = new UnclaimPackageResponseList();

            unclaimPackageResponseList.List = results;
            unclaimPackageResponseList.Count = count;
            return RickWebResult.Success(unclaimPackageResponseList);
        }

        /// <summary>
        /// 包裹认领
        /// </summary>
        /// <param name="unclaimPackagePostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] UnclaimPackagePostRequest unclaimPackagePostRequest)
        {
            await _expressclaimService.BeginTransactionAsync();
            Package package = await _expressclaimService.FindAsync<Package>(unclaimPackagePostRequest.Id);
            if (package.Expressnumber != unclaimPackagePostRequest.Expressnumber)
            {
                return RickWebResult.Error(new object(), 996, "快递单号错误");
            }
            Expressinfo expressinfo = new Expressinfo();
            expressinfo.Id = _idGenerator.NextId();
            expressinfo.Expressnumber = package.Expressnumber;
            expressinfo.Courierid = package.Courierid;
            expressinfo.Status = 1;
            expressinfo.Adduser = UserInfo.Id;
            expressinfo.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            expressinfo.Addtime = now;
            expressinfo.Lasttime = now;
            expressinfo.Source = 1;
            await _expressclaimService.AddAsync(expressinfo);

            Expressclaim expressclaim = new Expressclaim();
            expressclaim.Id = _idGenerator.NextId();
            expressclaim.Expressinfoid = expressinfo.Id;
            expressclaim.Repositoryid = package.Repositoryid;
            expressclaim.Appuser = UserInfo.Id;
            expressclaim.Remark = unclaimPackagePostRequest.Remark;
            expressclaim.Count = unclaimPackagePostRequest.Details.Count;
            expressclaim.Cansendasap = 0;
            if (package.Status == (int)PackageStatus.已入库)
            {
                expressclaim.Status = (int)ExpressClaimStatus.已入库;
            }
            else if (package.Status == (int)PackageStatus.已入柜)
            {
                expressclaim.Status = (int)ExpressClaimStatus.已揽收;
            }
            expressclaim.Adduser = UserInfo.Id;
            expressclaim.Lastuser = UserInfo.Id;
            expressclaim.Addtime = now;
            expressclaim.Lasttime = now;
            expressclaim.Packageid = package.Id;
            await _expressclaimService.AddAsync(expressclaim);

            Packagenote packagenote = new Packagenote();
            packagenote.Id = _idGenerator.NextId();
            packagenote.Packageid = (long)expressclaim.Packageid;
            packagenote.Status = 1;
            packagenote.Adduser = UserInfo.Id;
            packagenote.Addtime = now;
            packagenote.Isclosed = 0;
            packagenote.Operator = (int)PackageNoteStatus.认领;
            packagenote.Operatoruser = UserInfo.Id;
            await _expressclaimService.AddAsync(packagenote);

            foreach (UnclaimPackagePostDetailRequest unclaimPackagePostDetailRequest in unclaimPackagePostRequest.Details)
            {
                Expressclaimdetail expressclaimdetail = new Expressclaimdetail();
                expressclaimdetail.Id = _idGenerator.NextId();
                expressclaimdetail.Expressclaimid = expressclaim.Id;
                expressclaimdetail.Name = unclaimPackagePostDetailRequest.Name;
                expressclaimdetail.Unitprice = unclaimPackagePostDetailRequest.Unitprice;
                expressclaimdetail.Count = unclaimPackagePostDetailRequest.Count;
                expressclaimdetail.Status = 1;
                expressclaimdetail.Adduser = UserInfo.Id;
                expressclaimdetail.Lastuser = UserInfo.Id;
                expressclaimdetail.Addtime = now;
                expressclaimdetail.Lasttime = now;
                await _expressclaimService.AddAsync(expressclaimdetail);
            }
            package.Claimtype = 2;
            package.Name = string.Join(',', unclaimPackagePostRequest.Details.Select(t => t.Name));
            await _expressclaimService.UpdateAsync(package);

            await _expressclaimService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class UnclaimPackageResponse
        {
            public long Id { get; set; }
            public string Expressnumber { get; set; }
            public string CourierName { get; set; }
            public long? CourierId { get; set; }
            public DateTime Addtime { get; set; }
        }
        public class UnclaimPackageResponseList
        {
            public int Count { get; set; }
            public IList<UnclaimPackageResponse> List { get; set; }
        }
        public class UnclaimPackagePostRequest
        {
            public long Id { get; set; }
            public string Expressnumber { get; set; }
            public string Remark { get; set; }
            public List<UnclaimPackagePostDetailRequest> Details { get; set; }
        }
        public class UnclaimPackagePostDetailRequest
        {
            public string Name { get; set; }
            public decimal? Unitprice { get; set; }
            public int Count { get; set; }

        }


    }
}
