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
        /// <param name="code"></param>
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
        public async Task<RickWebResult<RepositoryInResponseList>> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] string expressNumber, [FromQuery] string inUserName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
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
                          && (string.IsNullOrEmpty(code) || t.Code == code)
                          && (t.Status >= (int)PackageStatus.已入库 && t.Status != (int)PackageStatus.已出库)
                          )
                        join courier in _packageService.Query<Courier>()
                        on package.Courierid equals courier.Id
                        join user in _packageService.Query<Sysuser>()
                        on package.Repositoryinuser equals user.Id
                        into userTemp
                        from user in userTemp.DefaultIfEmpty()
                        select new RepositoryInResponse()
                        {
                            Id = package.Id,
                            Repositoryid = package.Repositoryid,
                            Expressnumber = package.Expressnumber,
                            CourierId = courier.Id,
                            CourierName = courier.Name,
                            Code = package.Code,
                            Name = package.Name,
                            Weight = package.Weight,
                            Count = package.Count,
                            Inuser = package.Repositoryinuser,
                            Inusername = user == null ? string.Empty : user.Name,
                            Location = package.Location,
                            Addtime = package.Addtime,
                            Intime = package.Repositoryintime,
                            Status = package.Status,
                            Changecode = package.Changecode,
                            Refundcode = package.Refundcode,
                            Checkremark = package.Checkremark,
                            Refundremark = package.Refundremark,
                            Changeremark = package.Changeremark,
                            Repositoryregionid = package.Repositoryregionid,
                            Repositoryshelfid = package.Repositoryshelfid,
                            Repositorylayerid = package.Repositorylayerid,
                            Repositorynumber = package.Repositorynumber
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
            var relatedUserIds = users.Select(t => t.Id).ToList();
            //var relatedPackages = await (from claim in _packageService.Query<Expressclaim>(t => relatedUserIds.Contains(t.Appuser))
            //                             join package in _packageService.Query<Package>(t => t.Status == 2)
            //                             on claim.Packageid equals package.Id
            //                             select new
            //                             {
            //                                 package.Id,
            //                                 claim.Appuser,
            //                                 package.Location
            //                             }
            //            ).ToListAsync();

            var relatedPackages = await (from claim in _packageService.Query<Expressclaim>(t => relatedUserIds.Contains(t.Appuser))
                                         join package in _packageService.Query<Package>(t => t.Status == (int)PackageStatus.已入柜)
                                         on claim.Packageid equals package.Id
                                         join region in _packageService.Query<Repositoryregion>(t => t.Status == 1)
                                         on package.Repositoryregionid equals region.Id
                                         join shelf in _packageService.Query<Repositoryshelf>(t => t.Status == 1)
                                         on package.Repositoryshelfid equals shelf.Id
                                         join layer in _packageService.Query<Repositorylayer>(t => t.Status == 1)
                                         on package.Repositorylayerid equals layer.Id
                                         select new
                                         {
                                             package.Id,
                                             claim.Appuser,
                                             package.Location,
                                             Region = region,
                                             Shelf = shelf,
                                             Layer = layer,
                                             Package = package
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
                foreach (var user in packageInResponse.Users)
                {
                    user.RelatedLocations = relatedPackages.Where(t => t.Id != packageInResponse.Id && t.Appuser == user.Userid).Select(t => t.Location).ToList();
                    user.RelatedPackageLocations = relatedPackages.Where(t => t.Id != packageInResponse.Id && t.Appuser == user.Userid).Select(t => new RelatedLocationResponse() { 
                        Id = t.Id,
                        Repositoryregionid = t.Region.Id,
                        Repositoryregionname = t.Region.Name,
                        Repositoryshelfid = t.Shelf.Id,
                        Repositoryshelfname = t.Shelf.Name,
                        Repositorylayerid = t.Layer.Id,
                        Repositorylayername = t.Layer.Name,
                        Repositorynumber = t.Package.Repositorynumber
                    }).ToList();
                }
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
            if (package.Status != (int)PackageStatus.已入柜 || package.Status != (int)PackageStatus.已入库)
            {
                RickWebResult.Error<RepositoryInResponse>(null, 996, "包裹状态不正确");
            }
            Repositoryregion region = (await _packageService.QueryAsync<Repositoryregion>(t => t.Repositoryid == package.Repositoryid && t.Id == repositoryInRequest.Repositoryregionid)).FirstOrDefault();
            Repositoryshelf shelf = (await _packageService.QueryAsync<Repositoryshelf>(t => t.Repositoryid == package.Repositoryid && t.Id == repositoryInRequest.Repositoryshelfid)).FirstOrDefault();
            Repositorylayer layer = (await _packageService.QueryAsync<Repositorylayer>(t => t.Repositoryid == package.Repositoryid && t.Id == repositoryInRequest.Repositorylayerid)).FirstOrDefault();
            package.Repositoryregionid = region.Id;
            package.Repositoryshelfid = shelf.Id;
            package.Repositorylayerid = layer.Id;
            package.Repositorynumber = repositoryInRequest.Repositorynumber;
            string location = string.Format("{0}{1}{2}{3}", region.Name, shelf.Name, layer.Name, repositoryInRequest.Repositorynumber);
            package.Location = location;
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
            packagenote.Operator = (int)PackageNoteStatus.已入库;
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
            public long Repositoryregionid { get; set; }
            public long Repositoryshelfid { get; set; }
            public long Repositorylayerid { get; set; }
            public string Repositorynumber { get; set; }
        }

        public class RepositoryInResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }

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
            public string Changecode { get; set; }
            public string Refundcode { get; set; }

            public string Checkremark { get; set; }
            public string Refundremark { get; set; }
            public string Changeremark { get; set; }
            public long? Repositoryregionid { get; set; }
            public long? Repositoryshelfid { get; set; }
            public long? Repositorylayerid { get; set; }

            public string Repositorynumber { get; set; }

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
            public List<string> RelatedLocations { get; set; }
            public List<RelatedLocationResponse> RelatedPackageLocations { get; set; }
        }
        public class RelatedLocationResponse
        {
            public long Id { get; set; }
            public long Repositoryregionid { get; set; }
            public string Repositoryregionname { get; set; }
            public long Repositoryshelfid { get; set; }
            public string Repositoryshelfname { get; set; }
            public long Repositorylayerid { get; set; }
            public string Repositorylayername { get; set; }

            public string Repositorynumber { get; set; }

        }

        public class RepositoryInResponseList
        {
            public int Count { get; set; }
            public IList<RepositoryInResponse> List { get; set; }
        }

    }
}
