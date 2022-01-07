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
    public class PackagechangeController : RickControllerBase
    {
        private readonly ILogger<PackagechangeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;

        public PackagechangeController(ILogger<PackagechangeController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 包裹换货
        /// </summary>
        /// <param name="repositoryInPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] PackagechangePutRequest repositoryInPutRequest)
        {
            await _packageService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Package package = await _packageService.FindAsync<Package>(repositoryInPutRequest.Id);
            if (package.Status != (int)PackageStatus.待换货)
            {
                return RickWebResult.Error<object>(null, 996, "包裹状态不正确");
            }
            package.Status = (int)PackageStatus.已换货;
            await _packageService.UpdateAsync(package);

            var expressclaims = await _packageService.QueryAsync<Expressclaim>(t=>t.Packageid == package.Id);
            foreach (var expressclaim in expressclaims)
            {
                if (expressclaim.Status == (int)ExpressClaimStatus.待换货)
                {
                    expressclaim.Status = (int)ExpressClaimStatus.已换货;
                    await _packageService.UpdateAsync(expressclaim);
                }
            }

            foreach (var image in repositoryInPutRequest.Images)
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

            foreach (var vedio in repositoryInPutRequest.Videos)
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
            packagenote.Isclosed = 1;
            packagenote.Operator = (int)PackageNoteStatus.已换货;
            packagenote.Operatoruser = UserInfo.Id;
            await _packageService.AddAsync(packagenote);

            await _packageService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class PackagechangePutRequest
        {
            public long Id { get; set; }
            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }

    }
}
