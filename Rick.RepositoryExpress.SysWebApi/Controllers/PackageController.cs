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
    public class PackageController : RickControllerBase
    {
        private readonly ILogger<PackageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public PackageController(ILogger<PackageController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 库存查询
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="expressNumber"></param>
        /// <param name="addUser"></param>
        /// <param name="location"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<PackageResponseList>> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] string expressNumber, [FromQuery] string addUser, [FromQuery] string location, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            PackageResponseList packageResponseList = new PackageResponseList();
            packageResponseList.List = new List<PackageResponse>();

            var query = from package in _packageService.Query<Package>(t => (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        && (!status.HasValue || t.Status == status)
                        && (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                        && (string.IsNullOrEmpty(location) || t.Location.Contains(location))
                        )
                        join sysuser in _packageService.Query<Sysuser>(t => string.IsNullOrEmpty(addUser) || t.Name == addUser)
                        on package.Adduser equals sysuser.Id
                        join courier in _packageService.Query<Courier>()
                        on package.Courierid equals courier.Id
                        into courierTmp
                        from courier in courierTmp.DefaultIfEmpty()
                        join repository in _packageService.Query<Repository>()
                        on package.Repositoryid equals repository.Id
                        into repositoryTmp
                        from repository in repositoryTmp.DefaultIfEmpty()
                        select new PackageResponse()
                        {
                            Id = package.Id,
                            Repositoryid = package.Repositoryid,
                            RepositoryName = repository == null ? string.Empty : repository.Name,
                            Courierid = package.Courierid,
                            CourierName = courier == null ? string.Empty : courier.Name,
                            Expressnumber = package.Expressnumber,
                            Location = package.Location,
                            Name = package.Name,
                            Remark = package.Remark,
                            Status = package.Status,
                            Adduser = package.Adduser,
                            Addusername = sysuser.Name,
                            Addtime = package.Addtime,
                        };

            packageResponseList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            packageResponseList.Count = await query.CountAsync();

            IEnumerable<long> ids = packageResponseList.List.Select(t => t.Id);

            var imageInfos = await (from image in _packageService.Query<Packageimage>(t => ids.Contains(t.Packageid))
                                    select image
                                    ).ToListAsync();
            var vedioInfos = await (from vedio in _packageService.Query<Packagevideo>(t => ids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            foreach (var packageInResponse in packageResponseList.List)
            {
                packageInResponse.Images = imageInfos.Where(t => t.Packageid == packageInResponse.Id).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Videos = vedioInfos.Where(t => t.Packageid == packageInResponse.Id).Select(t => t.Fileinfoid).ToList();
            }


            return RickWebResult.Success(packageResponseList);
        }

        public class PackageResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string RepositoryName { get; set; }
            public long Courierid { get; set; }
            public string CourierName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public string Remark { get; set; }
            public int Status { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public List<long> Images { get; set; }
            public List<long> Videos { get; set; }


        }
        public class PackageResponseList
        {
            public int Count { get; set; }
            public List<PackageResponse> List { get; set; }
        }

    }
}
