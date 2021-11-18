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

namespace Rick.RepositoryExpress.WebApi.Controllers
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
        /// 包裹预报
        /// </summary>
        /// <param name="expressclaimRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] ExpressclaimRequest expressclaimRequest)
        {
            if (await _redisClientService.LockTakeAsync("Rick.RepositoryExpress.WebApi.Controllers.ClaimController.Post" + expressclaimRequest.Expressnumber, "Post"))
            {                
                Expressinfo expressinfo = await _expressclaimService.FindAsync<Expressinfo>(t => t.Expressnumber == expressclaimRequest.Expressnumber && t.Adduser == UserInfo.Id && t.Status == 1);
                if (expressinfo != null)
                {
                    return RickWebResult.Error(new object(), 996, "不能重复提交");
                }
                expressinfo = new Expressinfo();

                await _expressclaimService.BeginTransactionAsync();
                expressinfo.Id = _idGenerator.NextId();
                expressinfo.Expressnumber = expressclaimRequest.Expressnumber;
                expressinfo.Courierid = expressclaimRequest.Courierid;
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
                expressclaim.Repositoryid = expressclaimRequest.Repositoryid;
                expressclaim.Appuser = UserInfo.Id;
                expressclaim.Remark = expressclaimRequest.Remark;
                expressclaim.Count = expressclaimRequest.Count;
                expressclaim.Cansendasap = expressclaimRequest.Cansendasap;
                expressclaim.Status = (int)ExpressClaimStatus.正常;
                expressclaim.Adduser = UserInfo.Id;
                expressclaim.Lastuser = UserInfo.Id;
                expressclaim.Addtime = now;
                expressclaim.Lasttime = now;
                await _expressclaimService.AddAsync(expressclaim);

                foreach (ExpressclaimdetailRequest expressclaimdetailRequest in expressclaimRequest.details)
                {
                    Expressclaimdetail expressclaimdetail = new Expressclaimdetail();
                    expressclaimdetail.Id = _idGenerator.NextId();
                    expressclaimdetail.Expressclaimid = expressclaim.Id;
                    expressclaimdetail.Name = expressclaimdetailRequest.Name;
                    expressclaimdetail.Unitprice = expressclaimdetailRequest.Unitprice;
                    expressclaimdetail.Count = expressclaimdetailRequest.Count;
                    expressclaimdetail.Status = 1;
                    expressclaimdetail.Adduser = UserInfo.Id;
                    expressclaimdetail.Lastuser = UserInfo.Id;
                    expressclaimdetail.Addtime = now;
                    expressclaimdetail.Lasttime = now;
                    await _expressclaimService.AddAsync(expressclaimdetail);
                }
                await _expressclaimService.CommitAsync();
                return RickWebResult.Success(new object());
            }
            else
            {
                return RickWebResult.Error(new object(), 996, "不能重复提交");
            }
        }

        public class ExpressclaimRequest
        {
            public long Repositoryid { get; set; }
            public long Courierid { get; set; }
            public string Expressnumber { get; set; }
            public string Remark { get; set; }
            public int Count { get; set; }
            public sbyte Cansendasap { get; set; }
            public List<ExpressclaimdetailRequest> details { get; set; }
        }
        public class ExpressclaimdetailRequest
        {
            public string Name { get; set; }
            public decimal? Unitprice { get; set; }
            public int Count { get; set; }
        }

    }
}
