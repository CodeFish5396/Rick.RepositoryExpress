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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 入库录单，录入货物信息
    /// </summary>
    [Route("api/[controller]/{id?}")]
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

            int totalCount = 0;
            foreach (var packageDetailInRequest in packageInRequest.PackageDetails)
            {
                Packagedetail packagedetail = new Packagedetail();
                packagedetail.Id = _idGenerator.NextId();
                packagedetail.Packageid = package.Id;
                packagedetail.Name = packageDetailInRequest.Name;
                packagedetail.Unit = packageDetailInRequest.Unit;
                packagedetail.Count = packageDetailInRequest.Count;
                packagedetail.Status = 1;
                packagedetail.Adduser = UserInfo.Id;
                packagedetail.Lastuser = UserInfo.Id;
                packagedetail.Addtime = now;
                packagedetail.Lasttime = now;
                totalCount += packageDetailInRequest.Count;
                await _packageService.AddAsync(packagedetail);
            }
            package.Count = totalCount;
            await _packageService.AddAsync(package);

            var users = await _packageService.GetAppusers(packageInRequest.Expressnumber);
            foreach (var user in users)
            {
                Packageandexpressclaim packageandexpressclaim = new Packageandexpressclaim();
                packageandexpressclaim.Id = _idGenerator.NextId();
                packageandexpressclaim.Packageid = package.Id;
                packageandexpressclaim.Expressclaimid = user.ExpressclaimId;
                packageandexpressclaim.Status = 1;
                packageandexpressclaim.Adduser = UserInfo.Id;
                packageandexpressclaim.Lastuser = UserInfo.Id;
                packageandexpressclaim.Addtime = now;
                packageandexpressclaim.Lasttime = now;
                await _packageService.AddAsync(packageandexpressclaim);
            }

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
            await _packageService.CommitAsync();

            CommonResult commonResult = new CommonResult();
            commonResult.Id = package.Id;
            return RickWebResult.Success(commonResult);
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="expressNumber"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IList<PackageInView>>> Get([FromQuery] string expressNumber, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int index = 0, [FromQuery] int pageSize = 0)
        {
            var result = await _packageService.GetList(expressNumber, startTime.HasValue? startTime.Value:DateTime.MinValue, endTime.HasValue ? endTime.Value : DateTime.MinValue, index, pageSize);
            return RickWebResult.Success(result);
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[HttpGet]
        //public async Task<RickWebResult<CommonResult>> Get([FromQuery] long id = 0)
        //{

        //}

    }

    public class PackageInRequest
    {
        public long Repositoryid { get; set; }
        public string Expressnumber { get; set; }
        public long CourierId { get; set; }
        public string CourierName { get; set; }
        public IList<PackageDetailInRequest> PackageDetails { get; set; }
        public IList<long> Images { get; set; }
        public IList<long> Videos { get; set; }
    }

    public class PackageDetailInRequest
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Count { get; set; }
    }

}
