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
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApplyExpressExportController : RickControllerBase
    {
        private readonly ILogger<OrderApplyExpressController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public OrderApplyExpressExportController(ILogger<OrderApplyExpressController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
            var env = Environment.GetEnvironmentVariables();
            var os = Convert.ToString(env["OS"]);
            var dr = Convert.ToString(env["SystemDrive"]);
            if (os.Contains("Windows"))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                DirectoryInfo directory = new DirectoryInfo(currentDirectory);
                directory = directory.Parent;
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
                filePath = dr + "/Uploads/Temp/";
            }
        }

        /// <summary>
        /// 导出用户确认发货xlsx
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="sendUserName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string sendUserName)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (t.Orderstatus == (int)OrderApplyStatus.待发货 || t.Orderstatus == (int)OrderApplyStatus.已发货 || t.Orderstatus == (int)OrderApplyStatus.已签收 || t.Orderstatus == (int)OrderApplyStatus.问题件)
                        && (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1)
                        on order.Id equals packageorderapplyexpress.Packageorderapplyid
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join address in _packageOrderApplyService.Query<Appuseraddress>()
                        on order.Addressid equals address.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Senduser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        join courier in _packageOrderApplyService.Query<Courier>()
                        on packageorderapplyexpress.Courierid equals courier.Id
                        into courierTmp
                        from courier in courierTmp.DefaultIfEmpty()
                        join agent in _packageOrderApplyService.Query<Agent>()
                        on packageorderapplyexpress.Agentid equals agent.Id
                        into agentTmp
                        from agent in agentTmp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(sendUserName) || (sysUser != null && sysUser.Name == sendUserName))
                        select new OrderApplyExpressResponse()
                        {
                            Id = order.Id,
                            Expressid = packageorderapplyexpress.Id,
                            Code = order.Code,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            AddressName = address.Name,
                            AddressContactnumber = address.Contactnumber,
                            AddressRegion = address.Region,
                            AddressAddress = address.Address,
                            Appuser = order.Appuser,
                            Appusercode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed ?? 0,
                            Paytime = order.Paytime,
                            Price = packageorderapplyexpress.Price,
                            Freightprice = packageorderapplyexpress.Freightprice ?? 0,
                            Outnumber = packageorderapplyexpress.Outnumber,
                            Innernumber = packageorderapplyexpress.Innernumber,
                            Agentid = packageorderapplyexpress.Agentid,
                            Agentname = agent == null ? string.Empty : agent.Name,
                            Agentprice = packageorderapplyexpress.Agentprice,
                            Courierid = packageorderapplyexpress.Courierid,
                            Couriername = courier == null ? string.Empty : courier.Name,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Senduser = sysUser == null ? 0 : sysUser.Id,
                            Sendusername = sysUser == null ? string.Empty : sysUser.Name,
                            Sendtime = order.Sendtime,
                            Totalcount = packageorderapplyexpress.Totalcount ?? 0,
                            Totalweight = packageorderapplyexpress.Totalweight ?? 0,
                            Isagentpayed = order.Isagentpayed,
                            Agentpaytime = order.Agentpaytime,

                        };

  
            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();



            var userPayOrderids = list.Where(t => t.Ispayed == 1).Select(t => t.Id).ToList();
            var agentPayOrderids = list.Where(t => t.Isagentpayed == 1).Select(t => t.Id).ToList();

            var appuseraccountconsumes = await _packageOrderApplyService.QueryAsync<Appuseraccountconsume>(t => userPayOrderids.Contains((long)t.Orderid) && t.Status == 1);
            var agentfeeconsumes = await _packageOrderApplyService.QueryAsync<Agentfeeconsume>(t => agentPayOrderids.Contains((long)t.Orderid) && t.Status == 1);
            List<long> currenciesIds = new List<long>();
            List<long> userIds = new List<long>();


            foreach (var item in list)
            {
                if (item.Ispayed == 1)
                {
                    item.UserPay = appuseraccountconsumes.Where(t => t.Orderid == item.Id).Select(t => new UserPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Curencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.UserPay != null)
                    {
                        if (!currenciesIds.Contains(item.UserPay.Currencyid))
                        {
                            currenciesIds.Add(item.UserPay.Currencyid);
                        }
                        if (!userIds.Contains(item.UserPay.Userid))
                        {
                            userIds.Add(item.UserPay.Userid);
                        }
                    }
                }
                if (item.Isagentpayed == 1)
                {
                    item.AgentPay = agentfeeconsumes.Where(t => t.Orderid == item.Id).Select(t => new AgentPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Currencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.AgentPay != null)
                    {
                        if (!currenciesIds.Contains(item.AgentPay.Currencyid))
                        {
                            currenciesIds.Add(item.AgentPay.Currencyid);
                        }
                        if (!userIds.Contains(item.AgentPay.Userid))
                        {
                            userIds.Add(item.AgentPay.Userid);
                        }
                    }
                }

                var orderid = item.Id;
                var packageIds = await (from package in _packageOrderApplyService.Query<Packageorderapplydetail>(t => orderid == t.Packageorderapplyid && t.Status == 1)
                                        select package.Packageid
                        ).ToListAsync();

                var packages = await (from package in _packageOrderApplyService.Query<Package>(t => packageIds.Contains(t.Id))
                                      select package 
                            ).ToListAsync();
                item.Details = packages.Select(package => new OrderApplyExpressResponseDetail()
                {
                    PackageId = package.Id,
                    PackageCode = package.Code,
                    PackageName = package.Name,
                    Expressnumber = package.Expressnumber,
                    Location = package.Location,
                    Name = package.Name,
                    Count = package.Count,
                    Weight = package.Weight
                }).ToList();
                var imageInfos = await (from image in _packageOrderApplyService.Query<Packageimage>(t => packageIds.Contains(t.Packageid))
                                        select image
                            ).ToListAsync();

                var vedioInfos = await (from vedio in _packageOrderApplyService.Query<Packagevideo>(t => packageIds.Contains(t.Packageid))
                                        select vedio
                            ).ToListAsync();
                foreach (var Detail in item.Details)
                {
                    Detail.Images = imageInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                    Detail.Videos = vedioInfos.Where(t => t.Packageid == Detail.PackageId).Select(t => t.Fileinfoid).ToList();
                }

                var channelDetails = await (from cd in _packageOrderApplyService.Query<Channeldetail>(t => t.Nationid == item.Nationid && t.Channelid == item.Channelid && t.Status == 1)
                                            join agent in _packageOrderApplyService.Query<Agent>(t => t.Status == 1)
                                            on cd.Agentid equals agent.Id
                                            select new OrderApplyExpressResponseAgentDetail()
                                            {
                                                AgentId = agent.Id,
                                                AgentName = agent.Name
                                            }).ToListAsync();
                item.Agents = channelDetails;

                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Packageorderapplyid == orderid && t.Status == 1).SingleOrDefaultAsync();
                item.PostedDetails = new OrderApplyExpressPostResponse();
                if (packageorderapplyexpress != null)
                {
                    item.PostedDetails.Remark = packageorderapplyexpress.Remark;
                    item.PostedDetails.Price = packageorderapplyexpress.Price;
                    item.PostedDetails.Mailcode = packageorderapplyexpress.Mailcode;
                    var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
                    item.PostedDetails.Details = packageorderapplyexpressdetails.Select(t => new OrderApplyExpressPostResponsedetail()
                    {
                        Id = t.Id,
                        Count = t.Count,
                        Weight = t.Weight,
                        Customprice = t.Customprice,
                        Sueprice = t.Sueprice,
                        Overlengthprice = t.Overlengthprice,
                        Overweightprice = t.Overweightprice,
                        Oversizeprice = t.Oversizeprice,
                        Paperprice = t.Paperprice,
                        Boxprice = t.Boxprice,
                        Bounceprice = t.Bounceprice,
                        Vacuumprice = t.Vacuumprice,
                        PackAddPrice = t.Packaddprice,
                        HasPackAddPrice = t.Packaddprice.HasValue && t.Packaddprice != 0,
                        RemotePrice = t.Remoteprice,
                        HasRemote = t.Remoteprice.HasValue && t.Remoteprice != 0,
                        HasElectrified = t.Haselectrified.HasValue && t.Haselectrified != 0,
                        Price = t.Price,
                        Length = t.Length,
                        Width = t.Width,
                        Height = t.Height,
                        Volumeweight = t.Volumeweight
                    }).ToList();
                    foreach (var orderapplypostresponsedetail in item.PostedDetails.Details)
                    {
                        var packageexpress = await _packageOrderApplyService.Query<Packageorderapplyexpresspackage>(t => t.Packageorderapplyexpressdetailsid == orderapplypostresponsedetail.Id).ToListAsync();
                        var packageids = packageexpress.Select(t => t.Packageid);
                        orderapplypostresponsedetail.Packages = packages.Where(t => packageids.Contains(t.Id)).Select(package => new OrderApplyExpressPostResponseDetail()
                        {
                            PackageId = package.Id,
                            PackageName = package.Name,
                        }).ToList();
                    }
                    item.PostedDetails.Count = item.PostedDetails.Details.Count;
                    item.PostedDetails.TotalWeight = item.PostedDetails.Details.Sum(t => t.Weight ?? 0);
                }
            }
            
            var paycurrencies = await _packageOrderApplyService.QueryAsync<Currency>(t => currenciesIds.Contains(t.Id));
            var useres = await _packageOrderApplyService.QueryAsync<Sysuser>(t => userIds.Contains(t.Id));

            foreach (var item in list)
            {
                if (item.UserPay != null)
                {
                    item.UserPay.Currencyname = paycurrencies.Single(t => t.Id == item.UserPay.Currencyid).Name;
                    item.UserPay.Username = useres.Single(t => t.Id == item.UserPay.Userid).Name;
                }
                if (item.AgentPay != null)
                {
                    item.AgentPay.Currencyname = paycurrencies.Single(t => t.Id == item.AgentPay.Currencyid).Name;
                    item.AgentPay.Username = useres.Single(t => t.Id == item.AgentPay.Userid).Name;
                }
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("包裹上架");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("客户代码");
            head.CreateCell(1).SetCellValue("发货渠道");
            head.CreateCell(2).SetCellValue("收货国家");
            head.CreateCell(3).SetCellValue("内单号");
            head.CreateCell(4).SetCellValue("件数");
            head.CreateCell(5).SetCellValue("出重");
            head.CreateCell(6).SetCellValue("支付金额");
            head.CreateCell(7).SetCellValue("支付时间");
            head.CreateCell(8).SetCellValue("支付状态");
            head.CreateCell(9).SetCellValue("订单状态");
            head.CreateCell(10).SetCellValue("经手人");
            head.CreateCell(11).SetCellValue("发货时间");
            head.CreateCell(12).SetCellValue("追踪单号");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Appusercode);
                row.CreateCell(1).SetCellValue(item.Channelname);
                row.CreateCell(2).SetCellValue(item.Nationname);
                row.CreateCell(3).SetCellValue(item.Code);
                row.CreateCell(4).SetCellValue(item.Totalcount);
                row.CreateCell(5).SetCellValue(item.Totalweight.ToString());
                row.CreateCell(6).SetCellValue(item.Price.HasValue?item.Price.ToString():"无");
                row.CreateCell(7).SetCellValue(item.Paytime.HasValue ? item.Paytime.ToString() : "无");
                row.CreateCell(8).SetCellValue(item.Ispayed==1?"已支付":"未支付");
                row.CreateCell(9).SetCellValue(Enum.GetName(typeof(OrderApplyStatus), item.Orderstatus));
                row.CreateCell(10).SetCellValue(item.Sendusername);
                row.CreateCell(11).SetCellValue(item.Sendtime.ToString());
                row.CreateCell(12).SetCellValue(item.Outnumber);
                currentRow++;
            }


            string fileName = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                book.Write(fileStream);
            }
            #endregion
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fileStream.Length));

                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }
        }
  
        public class OrderApplyExpressResquest
        {
            public long Id { get; set; }
            public long Agentid { get; set; }
            public long Courierid { get; set; }
            public string Outnumber { get; set; }
            public decimal Agentprice { get; set; }
            public long Agentcurrencychangerateid { get; set; }
            public decimal Localagentprice { get; set; }
            public int Totalcount { get; set; }
            public decimal Totalweight { get; set; }

        }
        public class OrderApplyExpressResponse
        {
            public string Code { get; set; }
            public long Id { get; set; }
            public long Expressid { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public string AddressName { get; set; }
            public string AddressContactnumber { get; set; }
            public string AddressRegion { get; set; }
            public string AddressAddress { get; set; }

            public long Appuser { get; set; }
            public string Appusername { get; set; }
            public string Appusercode { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public decimal? Price { get; set; }
            public decimal? Freightprice { get; set; }

            public string Outnumber { get; set; }
            public string Innernumber { get; set; }
            public long? Agentid { get; set; }
            public string Agentname { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public decimal? Agentprice { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }
            public int Totalcount { get; set; }
            public decimal Totalweight { get; set; }
            public sbyte Isagentpayed { get; set; }
            public DateTime? Agentpaytime { get; set; }
            public UserPay UserPay { get; set; }
            public AgentPay AgentPay { get; set; }

            public List<OrderApplyExpressResponseAgentDetail> Agents { get; set; }
            public List<OrderApplyExpressResponseDetail> Details { get; set; }
            public OrderApplyExpressPostResponse PostedDetails { get; set; }


        }
        public class OrderApplyExpressResponseAgentDetail
        {
            public long AgentId { get; set; }
            public string AgentName { get; set; }
        }

        public class OrderApplyExpressResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageCode { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }

        public class OrderApplyExpressPutResquest
        {
            public long Id { get; set; }
        }
        public class OrderApplyExpressPostResponse
        {
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public int Count { get; set; }
            public decimal TotalWeight { get; set; }


            public IList<OrderApplyExpressPostResponsedetail> Details { get; set; }
        }
        public class OrderApplyExpressPostResponsedetail
        {
            public long Id { get; set; }
            public int? Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Customprice { get; set; }
            public decimal? Sueprice { get; set; }
            public decimal? Overlengthprice { get; set; }
            public decimal? Overweightprice { get; set; }
            public decimal? Oversizeprice { get; set; }
            public decimal? Vacuumprice { get; set; }
            public decimal? RemotePrice { get; set; }
            public bool HasElectrified { get; set; }
            public bool HasPackAddPrice { get; set; }
            public bool HasRemote { get; set; }
            public decimal? PackAddPrice { get; set; }

            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Price { get; set; }
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Volumeweight { get; set; }
            public List<OrderApplyExpressPostResponseDetail> Packages { get; set; }
        }
        public class OrderApplyExpressPostResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
        }
    }
}
