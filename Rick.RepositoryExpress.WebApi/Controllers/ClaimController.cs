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
                expressclaim.Status = (int)ExpressClaimStatus.预报;
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

        /// <summary>
        /// 查询包裹预报
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<ExpressclaimResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            await _expressclaimService.BeginTransactionAsync();
            ExpressclaimResponseList expressclaimResponseList = new ExpressclaimResponseList();

            var expressclaims = from claim in _expressclaimService.Query<Expressclaim>(t => t.Appuser == UserInfo.Id && t.Status != 0)
                                join expressinfo in _expressclaimService.Query<Expressinfo>(t => t.Status == 1)
                                on claim.Expressinfoid equals expressinfo.Id
                                orderby claim.Id descending
                                select new ExpressclaimResponse()
                                {
                                    Id = claim.Id,
                                    Claimid = claim.Id,
                                    Expressinfoid = claim.Expressinfoid,
                                    Expressnumber = expressinfo.Expressnumber,
                                    Repositoryid = claim.Repositoryid,
                                    Courierid = expressinfo.Courierid,
                                    Remark = claim.Remark,
                                    Packageid = claim.Packageid,
                                    ClaimStatus = claim.Status,
                                    Addtime = claim.Addtime
                                };

            expressclaimResponseList.Count = await expressclaims.CountAsync();
            expressclaimResponseList.List = expressclaims.Skip((index - 1) * pageSize).Take(pageSize).ToList();

            //获取PackageName
            var unnamedPackages = expressclaimResponseList.List.Where(t => t.ClaimStatus == 1).Select(t => t.Claimid).ToList();
            var expressclaimdetails = await _expressclaimService.QueryAsync<Expressclaimdetail>(t => t.Status == 1 && unnamedPackages.Contains(t.Expressclaimid));

            var packageIds = expressclaimResponseList.List.Where(t => t.ClaimStatus != 1).Select(t => t.Packageid).ToList();

            var packages = await _expressclaimService.QueryAsync<Package>(t => packageIds.Contains(t.Id));
            var packagenotes = await _expressclaimService.QueryAsync<Packagenote>(t => t.Operator <= (int)PackageNoteStatus.申请打包 && packageIds.Contains(t.Packageid));

            var packageorderapplydetailes = await _expressclaimService.QueryAsync<Packageorderapplydetail>(t => packageIds.Contains(t.Packageid));

            var packageorderapplyids = packageorderapplydetailes.Select(t => t.Packageorderapplyid).ToList();

            var packageorderapplies = await _expressclaimService.QueryAsync<Packageorderapply>(t => packageorderapplyids.Contains(t.Id));

            var addressIds = packageorderapplies.Select(t => t.Addressid).ToList();
            var appuseraddresses = await _expressclaimService.QueryAsync<Appuseraddress>(t => addressIds.Contains(t.Id));

            var nationids = appuseraddresses.Select(t => t.Nationid).ToList();
            var nations = await _expressclaimService.QueryAsync<Nation>(t => nationids.Contains(t.Id));

            var channelids = packageorderapplies.Select(t => t.Channelid).ToList();
            var channels = await _expressclaimService.QueryAsync<Channel>(t => channelids.Contains(t.Id));

            var packageorderapplyexpresses = await _expressclaimService.QueryAsync<Packageorderapplyexpress>(t => t.Status == 1 && packageorderapplyids.Contains(t.Packageorderapplyid));

            var repositoryids = expressclaimResponseList.List.Select(t => t.Repositoryid).ToList();
            var repositories = await _expressclaimService.QueryAsync<Repository>(t => repositoryids.Contains(t.Id));

            var outcourierids = packageorderapplyexpresses.Select(t => t.Courierid).ToList();
            var outcourieres = await _expressclaimService.QueryAsync<Courier>(t => outcourierids.Contains(t.Id));

            var courierids = expressclaimResponseList.List.Select(t => t.Courierid).ToList();
            var courieres = await _expressclaimService.QueryAsync<Courier>(t => courierids.Contains(t.Id));

            foreach (ExpressclaimResponse expressclaimResponse in expressclaimResponseList.List)
            {
                var currentRepository = repositories.SingleOrDefault(t => t.Id == expressclaimResponse.Repositoryid);
                expressclaimResponse.Repositoryname = currentRepository.Name;
                var currentCourier = courieres.SingleOrDefault(t => t.Id == expressclaimResponse.Courierid);
                expressclaimResponse.Couriername = currentCourier.Name;
                expressclaimResponse.ClaimStatusName = Enum.GetName(typeof(ExpressClaimStatus), expressclaimResponse.ClaimStatus);
                expressclaimResponse.Flows = packagenotes.Where(t => t.Packageid == expressclaimResponse.Packageid).Select(t => new ExpressclaimDetailsResponse()
                {
                    Packageid = expressclaimResponse.Packageid,
                    Addtime = t.Addtime,
                    Operator = t.Operator,
                    Operatorname = Enum.GetName(typeof(PackageNoteStatus), t.Operator)

                }).OrderBy(t => t.Addtime).ToList();

                if (expressclaimResponse.ClaimStatus == (int)ExpressClaimStatus.预报)
                {
                    expressclaimResponse.Packagename = string.Join(',', expressclaimdetails.Where(t => t.Expressclaimid == expressclaimResponse.Claimid).Select(t => t.Name));
                }
                else
                {
                    Package currentPackage = packages.SingleOrDefault(t => t.Id == expressclaimResponse.Packageid);
                    expressclaimResponse.Packagename = currentPackage.Name;
                    expressclaimResponse.Packagecode = currentPackage.Code;
                    if (expressclaimResponse.ClaimStatus >= (int)ExpressClaimStatus.申请打包)
                    {
                        Packageorderapplydetail currentpackageorderapplydetail = packageorderapplydetailes.FirstOrDefault(t => t.Packageid == currentPackage.Id);

                        Packageorderapply currentpackageorderapply = packageorderapplies.FirstOrDefault(t => t.Id == currentpackageorderapplydetail.Packageorderapplyid);

                        expressclaimResponse.Packageorderapplyid = currentpackageorderapply.Id;
                        expressclaimResponse.Packageorderapplystatus = currentpackageorderapply.Orderstatus;
                        expressclaimResponse.Packageorderapplystatusname = Enum.GetName(typeof(ExpressClaimStatus), currentpackageorderapply.Orderstatus);
                        expressclaimResponse.Addressid = currentpackageorderapply.Addressid;
                        Appuseraddress currentappuseraddress = appuseraddresses.SingleOrDefault(t => t.Id == currentpackageorderapply.Addressid);
                        expressclaimResponse.Addressname = currentappuseraddress.Name;
                        expressclaimResponse.Addresscontactnumber = currentappuseraddress.Contactnumber;
                        expressclaimResponse.Addressregion = currentappuseraddress.Region;
                        expressclaimResponse.Addressaddress = currentappuseraddress.Address;
                        expressclaimResponse.Nationid = currentappuseraddress.Nationid;

                        Nation nation = nations.SingleOrDefault(t => t.Id == currentappuseraddress.Nationid);
                        expressclaimResponse.Nationname = nation.Name;
                        expressclaimResponse.Channelid = currentpackageorderapply.Channelid;
                        Channel channel = channels.SingleOrDefault(t => t.Id == currentpackageorderapply.Channelid);
                        expressclaimResponse.Channelname = channel.Name;
                        if (expressclaimResponse.ClaimStatus >= (int)ExpressClaimStatus.已发货)
                        {
                            Packageorderapplyexpress currentpackageorderapplyexpress = packageorderapplyexpresses.FirstOrDefault(t => t.Packageorderapplyid == currentpackageorderapply.Id);
                            expressclaimResponse.ExpressId = currentpackageorderapplyexpress.Id;
                            expressclaimResponse.OutCourierid = currentpackageorderapplyexpress.Courierid;
                            expressclaimResponse.ExpressId = currentpackageorderapplyexpress.Id;
                            Courier outCourier = outcourieres.SingleOrDefault(t => t.Id == currentpackageorderapplyexpress.Courierid);
                            expressclaimResponse.OutCouriername = outCourier.Name;
                            expressclaimResponse.Outnumber = currentpackageorderapplyexpress.Outnumber;
                        }
                    }
                }
            }


            await _expressclaimService.CommitAsync();

            return RickWebResult.Success(expressclaimResponseList);
        }
        public class ExpressclaimResponseList
        {
            public int Count { get; set; }
            public List<ExpressclaimResponse> List { get; set; }

        }
        public class ExpressclaimResponse
        {
            public long Id { get; set; }
            public long Claimid { get; set; }
            public long Expressinfoid { get; set; }
            public string Expressnumber { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }
            public string Remark { get; set; }
            public int ClaimStatus { get; set; }
            public string ClaimStatusName { get; set; }
            public long? Packageid { get; set; }
            public string Packagename { get; set; }
            public string Packagecode { get; set; }
            public long? Packageorderapplyid { get; set; }
            public int Packageorderapplystatus { get; set; }
            public string Packageorderapplystatusname { get; set; }
            public long? Addressid { get; set; }
            public string Addressname { get; set; }
            public string Addresscontactnumber { get; set; }
            public string Addressregion { get; set; }
            public string Addressaddress { get; set; }
            public long? Nationid { get; set; }
            public string Nationname { get; set; }
            public long? Channelid { get; set; }
            public string Channelname { get; set; }
            public long ExpressId { get; set; }
            public long? OutCourierid { get; set; }
            public string OutCouriername { get; set; }
            public string Outnumber { get; set; }
            public DateTime Addtime { get; set; }

            public List<ExpressclaimDetailsResponse> Flows { get; set; }

        }
        public class ExpressclaimDetailsResponse
        {
            public long? Packageid { get; set; }
            public DateTime Addtime { get; set; }
            public int Operator { get; set; }
            public string Operatorname { get; set; }

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
