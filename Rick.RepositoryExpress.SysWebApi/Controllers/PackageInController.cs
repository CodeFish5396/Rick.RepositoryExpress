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
using Rick.RepositoryExpress.DataBase.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 入库录单，录入货物信息
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PackageInController : RickControllerBase
    {
        private readonly ILogger<PackageInPreController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public PackageInController(ILogger<PackageInPreController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 包裹入库，录入货物信息
        /// </summary>
        /// <param name="packageInRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<CommonResult>> Post([FromBody] PackageInRequest packageInRequest)
        {
            await _packageService.BeginTransactionAsync();
            Package package = new Package();
            package.Id = _idGenerator.NextId();

            package.Expressnumber = packageInRequest.Expressnumber;
            package.Repositoryid = packageInRequest.Repositoryid;
            package.Courierid = packageInRequest.CourierId;
            package.Status = 1;
            package.Adduser = UserInfo.Id;
            package.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            package.Addtime = now;
            package.Lasttime = now;
            package.Count = packageInRequest.Count;
            package.Name = packageInRequest.Name;
            package.Weight = packageInRequest.Weight;


            Expressinfo expressinfo = new Expressinfo();

            expressinfo.Id = _idGenerator.NextId();
            expressinfo.Expressnumber = packageInRequest.Expressnumber;
            expressinfo.Status = 1;
            expressinfo.Adduser = UserInfo.Id;
            expressinfo.Lastuser = UserInfo.Id;
            expressinfo.Addtime = now;
            expressinfo.Lasttime = now;
            expressinfo.Source = 2;
            await _packageService.AddAsync(expressinfo);

            package.Expressinfoid = expressinfo.Id;

            await _packageService.AddAsync(package);

            var expressclaims = await _packageService.GetExpressclaims(packageInRequest.Expressnumber);
            foreach (var expressclaim in expressclaims)
            {
                expressclaim.Packageid = package.Id;
                expressclaim.Status = (int)ExpressClaimStatus.已到库;
                await _packageService.UpdateAsync(expressclaim);
            }
            //foreach (var detail in packageInRequest.Details)
            //{
            //    Packagedetail packagedetail = new Packagedetail();
            //    packagedetail.Id = _idGenerator.NextId();
            //    packagedetail.Packageid = package.Id;
            //    packagedetail.Status = 1;
            //    packagedetail.Adduser = UserInfo.Id;
            //    packagedetail.Addtime = now;
            //    packagedetail.Count = detail.Count;
            //    packagedetail.Name = detail.Name;
            //    packagedetail.Unit = detail.Unit;
            //    await _packageService.AddAsync(packagedetail);
            //}

            foreach (var image in packageInRequest.Images)
            {
                Packageimage packageimage = new Packageimage();
                packageimage.Id = _idGenerator.NextId();
                packageimage.Packageid = package.Id;
                packageimage.Fileinfoid = image;
                packageimage.Status = 1;
                packageimage.Adduser = UserInfo.Id;
                packageimage.Addtime = now;
                await _packageService.AddAsync(packageimage);
            }

            foreach (var vedio in packageInRequest.Videos)
            {
                Packagevideo packagevideo = new Packagevideo();
                packagevideo.Id = _idGenerator.NextId();
                packagevideo.Packageid = package.Id;
                packagevideo.Fileinfoid = vedio;
                packagevideo.Status = 1;
                packagevideo.Adduser = UserInfo.Id;
                packagevideo.Addtime = now;
                await _packageService.AddAsync(packagevideo);
            }

            Packagenote packagenote = new Packagenote();
            packagenote.Id = _idGenerator.NextId();
            packagenote.Packageid = package.Id;
            packagenote.Status = 1;
            packagenote.Adduser = UserInfo.Id;
            packagenote.Addtime = now;
            packagenote.Isclosed = 0;
            packagenote.Operator = (int)PackageNoteStatus.入库录单;
            packagenote.Operatoruser = UserInfo.Id;
            await _packageService.AddAsync(packagenote);

            await _packageService.CommitAsync();

            CommonResult commonResult = new CommonResult();
            commonResult.Id = package.Id;
            return RickWebResult.Success(commonResult);
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="expressNumber"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<PackageInResponseList>> Get([FromQuery] long? id, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] string expressNumber, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            PackageInResponseList packageInResponseList = new PackageInResponseList();

            var baseQuery = from package in _packageService.Query<Package>(t => t.Status == 1 && (!id.HasValue || t.Id == id) && (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber) && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                            join exclaim in _packageService.Query<Expressclaim>()
                            on package.Id equals exclaim.Packageid
                            into exclaimtemp
                            from exclaimt in exclaimtemp.DefaultIfEmpty()
                            join user in _packageService.Query<Appuser>()
                            on exclaimt.Appuser equals user.Id
                            into usertemp
                            from usert in usertemp.DefaultIfEmpty()
                            join courier in _packageService.Query<Courier>()
                            on package.Courierid equals courier.Id
                            into courierTemp
                            from courier in courierTemp.DefaultIfEmpty()
                            join sysuser in _packageService.Query<Sysuser>()
                            on package.Lastuser equals sysuser.Id
                            where (string.IsNullOrEmpty(userCode) || (usert != null && usert.Usercode == userCode))
                            && (string.IsNullOrEmpty(userName) || (usert != null && usert.Truename == userName))
                            && (string.IsNullOrEmpty(userMobile) || (usert != null && usert.Mobile == userMobile))
                            select new PackageInResponse()
                            {
                                Userid = usert == null ? 0 : usert.Id,
                                Usercode = usert == null ? string.Empty : usert.Usercode,
                                Username = usert == null ? string.Empty : usert.Name,
                                Usertruename = usert == null ? string.Empty : usert.Truename,
                                Userphone = usert == null ? string.Empty : usert.Mobile,
                                Packageid = package.Id,
                                Count = package.Count,
                                Name = package.Name,
                                Weight = package.Weight,
                                Claimtime = exclaimt == null ? string.Empty : exclaimt.Addtime.ToString("yyyy-MM-dd HH:mm:ss"),
                                Addtime = package.Addtime,
                                Courierid = package.Courierid,
                                Couriername = courier == null ? string.Empty : courier.Name,
                                Expressnumber = package.Expressnumber,
                                Lastuser = package.Lastuser,
                                Lastusername = sysuser.Name,
                                Lasttime = package.Lasttime
                            };

            packageInResponseList.Count = await baseQuery.CountAsync();
            packageInResponseList.List = await baseQuery.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            IEnumerable<long> ids = packageInResponseList.List.Select(t => t.Packageid);

            var imageInfos = await (from image in _packageService.Query<Packageimage>(t => ids.Contains(t.Packageid))
                                    select image
                                    ).ToListAsync();
            var vedioInfos = await (from vedio in _packageService.Query<Packagevideo>(t => ids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            foreach (var packageInResponse in packageInResponseList.List)
            {
                packageInResponse.Images = imageInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Videos = vedioInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
            }

            return RickWebResult.Success(packageInResponseList);
        }

    }

    public class PackageInRequest
    {
        public long Repositoryid { get; set; }
        public string Expressnumber { get; set; }
        public long CourierId { get; set; }
        public string CourierName { get; set; }
        public string Name { get; set; }
        public decimal Weight { get; set; }
        public int Count { get; set; }
        public IList<long> Images { get; set; }
        public IList<long> Videos { get; set; }
        //public IList<PackageDetailInRequest> Details { get; set; }

    }

    public class PackageDetailInRequest
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Count { get; set; }
    }
    public class PackageInResponseList
    {
        public int Count { get; set; }
        public IList<PackageInResponse> List { get; set; }
    }
    public class PackageInResponse
    {
        public long Userid { get; set; }
        public string Usercode { get; set; }

        public long Packageid { get; set; }
        public string Name { get; set; }
        public decimal? Weight { get; set; }

        public int Count { get; set; }
        public string Username { get; set; }
        public string Usertruename { get; set; }
        public string Userphone { get; set; }
        public string Claimtime { get; set; }
        public DateTime Addtime { get; set; }
        public DateTime Lasttime { get; set; }
        public long Courierid { get; set; }
        public string Couriername { get; set; }
        public string Expressnumber { get; set; }
        public long Lastuser { get; set; }
        public string Lastusername { get; set; }
        public List<long> Images { get; set; }
        public List<long> Videos { get; set; }

    }

}
