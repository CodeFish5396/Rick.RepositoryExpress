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
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimController : RickControllerBase
    {
        private readonly ILogger<ClaimController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public ClaimController(ILogger<ClaimController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户包裹预报
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="expressNumber"></param>
        /// <param name="userCode"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<ExpressclaimResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] string expressNumber, [FromQuery] string userCode, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            ExpressclaimResponseList expressclaimResponseList = new ExpressclaimResponseList();
            var mainQuery = from expressclaim in _expressclaimService.Query<Expressclaim>(t => (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime) && (!status.HasValue || t.Status == status))
                            join expressinfo in _expressclaimService.Query<Expressinfo>(t => string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                            on expressclaim.Expressinfoid equals expressinfo.Id
                            join appuser in _expressclaimService.Query<Appuser>(t => string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            on expressclaim.Appuser equals appuser.Id
                            select new ExpressclaimResponse()
                            {
                                Id = expressclaim.Id,
                                Expressinfoid = expressclaim.Expressinfoid,
                                Expressnumber = expressinfo.Expressnumber,
                                Courierid = expressinfo.Courierid,
                                Packageid = expressclaim.Packageid,
                                Repositoryid = expressclaim.Repositoryid,
                                Appuser = expressclaim.Appuser,
                                Usercode = appuser.Usercode,
                                Username = appuser.Name,
                                Remark = expressclaim.Remark,
                                Count = expressclaim.Count,
                                Status = expressclaim.Status,
                                Addtime = expressclaim.Addtime
                            };
            expressclaimResponseList.Count = await mainQuery.CountAsync();
            expressclaimResponseList.List = await mainQuery.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            var courierids = expressclaimResponseList.List.Select(t => t.Courierid).Distinct();
            var couriers = await _expressclaimService.QueryAsync<Courier>(t => courierids.Contains(t.Id));

            var packageids = expressclaimResponseList.List.Select(t => t.Packageid).Distinct();
            var packages = await _expressclaimService.QueryAsync<Package>(t => packageids.Contains(t.Id));

            var repositorieids = expressclaimResponseList.List.Select(t => t.Repositoryid).Distinct();
            var repositories = await _expressclaimService.QueryAsync<Repository>(t => repositorieids.Contains(t.Id));
            foreach (var expressclaimresponse in expressclaimResponseList.List)
            {
                var currentCourier = couriers.SingleOrDefault(t => t.Id == expressclaimresponse.Courierid);
                expressclaimresponse.Couriername = currentCourier == null ? string.Empty : currentCourier.Name;
                var currentPackage = packages.SingleOrDefault(t => t.Id == expressclaimresponse.Packageid);
                expressclaimresponse.Packagename = currentPackage == null ? string.Empty : currentPackage.Name;
                expressclaimresponse.Location = currentPackage == null ? string.Empty : currentPackage.Location;
                expressclaimresponse.Repositoryname = repositories.Single(t => t.Id == expressclaimresponse.Repositoryid).Name;
            }
            return RickWebResult.Success(expressclaimResponseList);
        }
        public class ExpressclaimResponse
        {
            public long Id { get; set; }
            public long Expressinfoid { get; set; }
            public string Expressnumber { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public long? Packageid { get; set; }
            public string Packagename { get; set; }
            public string Location { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }
            public long Appuser { get; set; }
            public string Usercode { get; set; }
            public string Username { get; set; }
            public string Remark { get; set; }
            public int Count { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public List<ExpressclaimResponseDetail> Detail { get; set; }
        }
        public class ExpressclaimResponseDetail
        {

        }

        public class ExpressclaimResponseList
        {
            public int Count { get; set; }
            public IList<ExpressclaimResponse> List { get; set; }
        }


    }
}
