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
    public class RepositoryInController : RickControllerBase
    {
        private readonly ILogger<RepositoryInController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public RepositoryInController(ILogger<RepositoryInController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询包裹入库
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="expressNumber"></param>
        /// <param name="inUserName"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositoryInResponseList>> Get([FromQuery] long? id, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] string expressNumber, [FromQuery] string inUserName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            bool isUser = !(string.IsNullOrEmpty(userCode) && string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userMobile));
            List<long> packageids = new List<long>();
            if (isUser)
            {
                var appUsers = await (from appuser in _packageService.Query<Appuser>(t =>
                                (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                                && (string.IsNullOrEmpty(userName) || t.Name == userName)
                                && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                              )
                                      select appuser.Id).ToListAsync();
                packageids = await (from claim in _packageService.Query<Expressclaim>(t => appUsers.Contains(t.Appuser) && t.Packageid.HasValue)
                                    select (long)claim.Packageid).ToListAsync();

            }
            var query = from package in _packageService.Query<Package>(t => (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                          && (!startTime.HasValue || t.Addtime >= startTime)
                          && (!endTime.HasValue || t.Addtime <= endTime)
                          && (!id.HasValue || t.Addtime >= startTime)
                          && (t.Status == (int)PackageStatus.已入库 || t.Status == (int)PackageStatus.已入柜)
                          )
                        join user in _packageService.Query<Sysuser>()
                        on package.Repositoryinuser equals user.Id 
                        into userTemp
                        from user in userTemp.DefaultIfEmpty()
                        select new RepositoryInResponse()
                        {
                            Id = package.Id,
                            Expressnumber = package.Expressnumber,
                            Code = package.Code,
                            Name = package.Name,
                            Weight = package.Weight,
                            Count = package.Count,
                            Inuser = package.Repositoryinuser,
                            Inusername = user == null ? string.Empty : user.Name,
                            Location = package.Location,
                            Addtime = package.Addtime,
                            Intime = package.Repositoryintime,
                            Status = package.Status
                        };
            if (isUser)
            {
                query = from repositoryinresponse in query
                        where packageids.Contains(repositoryinresponse.Id)
                        select repositoryinresponse;
            }
            if (!string.IsNullOrEmpty(inUserName))
            {
                query = from repositoryinresponse in query
                        where repositoryinresponse.Inusername == inUserName
                        select repositoryinresponse;
            }
            RepositoryInResponseList repositoryInResponceList = new RepositoryInResponseList();
            repositoryInResponceList.Count = await query.CountAsync();
            repositoryInResponceList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            IEnumerable<long> ids = repositoryInResponceList.List.Select(t => t.Id);

            var imageInfos = await (from image in _packageService.Query<Packageimage>(t => ids.Contains(t.Packageid))
                                    select image
                                    ).ToListAsync();

            var vedioInfos = await (from vedio in _packageService.Query<Packagevideo>(t => ids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            var users = await (from claim in _packageService.Query<Expressclaim>(t => t.Packageid.HasValue && ids.Contains((long)t.Packageid))
                               join user in _packageService.Query<Appuser>()
                               on claim.Appuser equals user.Id
                               select new
                               {
                                   claim.Packageid,
                                   user.Id,
                                   user.Usercode,
                                   user.Truename,
                                   user.Name,
                                   user.Mobile
                               }
                        ).ToListAsync();

            foreach (var packageInResponse in repositoryInResponceList.List)
            {
                packageInResponse.Images = imageInfos.Where(t => t.Packageid == packageInResponse.Id).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Videos = vedioInfos.Where(t => t.Packageid == packageInResponse.Id).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Users = users.Where(t => t.Packageid == packageInResponse.Id).Select(t => new RepositoryInUserInfoResponse()
                {
                    Userid = t.Id,
                    Usercode = t.Usercode,
                    Username = t.Truename,
                    Usermobile = t.Mobile
                }).ToList();
            }

            return RickWebResult.Success(repositoryInResponceList);
        }

        /// <summary>
        /// 包裹入库录单
        /// </summary>
        /// <param name="repositoryInRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<RepositoryInResponse>> Post([FromBody] RepositoryInRequest repositoryInRequest)
        {
            await _packageService.BeginTransactionAsync();
            Package package = await _packageService.FindAsync<Package>(repositoryInRequest.Id);
            package.Location = repositoryInRequest.Location;
            DateTime now = DateTime.Now;
            package.Lasttime = now;
            package.Lastuser = UserInfo.Id;
            package.Status = (int)PackageStatus.已入柜;
            package.Repositoryintime = now;
            package.Repositoryinuser = UserInfo.Id;
            await _packageService.UpdateAsync(package);

            Packagenote packagenote = new Packagenote();
            packagenote.Id = _idGenerator.NextId();
            packagenote.Packageid = package.Id;
            packagenote.Status = 1;
            packagenote.Adduser = UserInfo.Id;
            packagenote.Addtime = now;
            packagenote.Isclosed = 0;
            packagenote.Operator = (int)PackageNoteStatus.包裹入库;
            packagenote.Operatoruser = UserInfo.Id;
            await _packageService.AddAsync(packagenote);

            var expressclaims = await (from ec in _packageService.Query<Expressclaim>(t => 1 == 1 && t.Packageid == package.Id)
                                       select ec).ToListAsync();
            foreach (var expressclaim in expressclaims)
            {
                expressclaim.Status = (int)ExpressClaimStatus.已入库;
                await _packageService.UpdateAsync(expressclaim);
            }

            await _packageService.CommitAsync();

            RepositoryInResponse repositoryInResponce = new RepositoryInResponse()
            {
                Id = package.Id,
                Expressnumber = package.Expressnumber,
                Code = package.Code,
                CourierId = package.Courierid,
                Name = package.Name,
                Weight = package.Weight,
                Count = package.Count,
                Inuser = package.Lastuser,
                Inusername = UserInfo.Name,
                Intime = now,
                Location = package.Location
            };

            return RickWebResult.Success(repositoryInResponce);
        }

        public class RepositoryInRequest
        {
            public long Id { get; set; }
            public string Location { get; set; }
        }

        public class RepositoryInResponse
        {
            public long Id { get; set; }
            public string Expressnumber { get; set; }
            public string Code { get; set; }
            public long CourierId { get; set; }
            public string CourierName { get; set; }
            public string Name { get; set; }
            public decimal? Weight { get; set; }
            public int Count { get; set; }
            public long? Inuser { get; set; }
            public string Inusername { get; set; }
            public string Location { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime? Intime { get; set; }
            public int Status { get; set; }
            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
            public IList<RepositoryInUserInfoResponse> Users { get; set; }
        }

        public class RepositoryInUserInfoResponse
        {
            public long Userid { get; set; }
            public string Usercode { get; set; }
            public string Username { get; set; }
            public string Usermobile { get; set; }

        }

        public class RepositoryInResponseList
        {
            public int Count { get; set; }
            public IList<RepositoryInResponse> List { get; set; }
        }
    }
}
