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
    public class PackagechangeController : RickControllerBase
    {
        private readonly ILogger<PackagechangeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;

        public PackagechangeController(ILogger<PackagechangeController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 申请换货
        /// </summary>
        /// <param name="id"></param>
        /// <param name="code"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromQuery] long id, [FromQuery] string code, [FromQuery] string remark)
        {
            await _expressclaimService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Expressclaim expressclaim = await _expressclaimService.FindAsync<Expressclaim>(id);
            if (expressclaim.Status != (int)ExpressClaimStatus.已入库 && expressclaim.Status != (int)ExpressClaimStatus.已验货)
            {
                RickWebResult.Error(new object(), 996, "单据状态错误");
            }

            Package package = await _expressclaimService.FindAsync<Package>((long)expressclaim.Packageid);
            if (package.Status != (int)PackageStatus.已入柜 && package.Status != (int)PackageStatus.已验货)
            {
                RickWebResult.Error(new object(), 996, "单据状态错误");
            }

            expressclaim.Status = (int)ExpressClaimStatus.待换货;
            await _expressclaimService.UpdateAsync(expressclaim);
            package.Status = (int)PackageStatus.待换货;
            package.Changecode = code;
            package.Changeremark = remark;
            await _expressclaimService.UpdateAsync(package);

            Packagenote packagenote = new Packagenote();
            packagenote.Id = _idGenerator.NextId();
            packagenote.Packageid = package.Id;
            packagenote.Status = 1;
            packagenote.Adduser = UserInfo.Id;
            packagenote.Addtime = now;
            packagenote.Isclosed = 0;
            packagenote.Operator = (int)PackageNoteStatus.待换货;
            packagenote.Operatoruser = UserInfo.Id;
            await _expressclaimService.AddAsync(packagenote);

            await _expressclaimService.CommitAsync();
            return RickWebResult.Success(new object());
        }

    }
}
