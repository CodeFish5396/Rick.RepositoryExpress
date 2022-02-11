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
    public class OrderApplyExportController : RickControllerBase
    {
        private readonly ILogger<OrderApplyController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public OrderApplyExportController(ILogger<OrderApplyController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
        /// 导出用户的申请打包xlsx
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="packageUserName"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string packageUserName)
        {
            #region 读取数据
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        && (t.Orderstatus == (int)OrderApplyStatus.申请打包 || t.Orderstatus == (int)OrderApplyStatus.发货待确认 || t.Orderstatus == (int)OrderApplyStatus.问题件)
                        )
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t =>
                            (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Packuser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(packageUserName) || (sysUser != null && sysUser.Name == packageUserName))
                        select new OrderApplyResponse()
                        {
                            Code = order.Code,
                            Id = order.Id,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Channelprice = channel.Unitprice,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            Appuser = order.Appuser,
                            AppuserCode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed,
                            Paytime = order.Paytime,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Packagetime = order.Packtime,
                            Packageuser = sysUser == null ? 0 : sysUser.Id,
                            Packageusername = sysUser == null ? string.Empty : sysUser.Name,
                        };

            OrderApplyResponseList orderApplyResponseList = new OrderApplyResponseList();
            orderApplyResponseList.Count = await query.CountAsync();
            orderApplyResponseList.List = await query.OrderByDescending(t => t.Id).ToListAsync();

            var chaneelIds = orderApplyResponseList.List.Select(t => t.Channelid).ToList();
            var channelPrices = await _packageOrderApplyService.QueryAsync<Channelprice>(t => t.Status == 1 && chaneelIds.Contains(t.Channelid));
            foreach (OrderApplyResponse orderApplyResponse in orderApplyResponseList.List)
            {
                orderApplyResponse.Pricedetails = channelPrices.Where(t => t.Channelid == orderApplyResponse.Channelid && t.Nationid == orderApplyResponse.Nationid).Select(t => new ChannelResponsepricedetail()
                {
                    Id = t.Id,
                    Channelid = t.Channelid,
                    Nationid = t.Nationid,
                    Minweight = t.Minweight,
                    Maxweight = t.Maxweight,
                    Price = t.Price
                }).ToList();
            }

            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);
            Currencychangerate currentRate = await _packageOrderApplyService.Query<Currencychangerate>(t => t.Status == 1 && t.Sourcecurrency == defaultCurrency.Id && t.Targetcurrency == localCurrency.Id).SingleAsync();
            orderApplyResponseList.Currencychangerateid = currentRate.Id;
            orderApplyResponseList.Currencychangerate = currentRate.Rate;

            foreach (var item in orderApplyResponseList.List)
            {
                var orderid = item.Id;
                var packageIds = await (from package in _packageOrderApplyService.Query<Packageorderapplydetail>(t => orderid == t.Packageorderapplyid && t.Status == 1)
                                        select package.Packageid
                        ).ToListAsync();

                var packages = await (from package in _packageOrderApplyService.Query<Package>(t => packageIds.Contains(t.Id))
                                      select package
                            ).ToListAsync();
                item.Details = packages.Select(package => new OrderApplyResponseDetail()
                {
                    PackageId = package.Id,
                    PackageCode = package.Code,
                    PackageName = package.Name,
                    Expressnumber = package.Expressnumber,
                    Location = package.Location,
                    Name = package.Name,
                    Count = package.Count,
                    Weight = package.Weight,
                    Payedprice = package.Freightprice
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

                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Packageorderapplyid == orderid && t.Status == 1).SingleOrDefaultAsync();
                item.PostedDetails = new OrderApplyPostResponse();
                if (packageorderapplyexpress != null)
                {
                    item.PostedDetails.Remark = packageorderapplyexpress.Remark;
                    item.PostedDetails.Price = packageorderapplyexpress.Price;
                    item.PostedDetails.Mailcode = packageorderapplyexpress.Mailcode;
                    var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
                    item.PostedDetails.Details = packageorderapplyexpressdetails.Select(t => new OrderApplyPostResponsedetail()
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
                        HasPackAddPrice = t.Packaddprice.HasValue && t.Packaddprice !=0,
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
                        orderapplypostresponsedetail.Packages = packages.Where(t => packageids.Contains(t.Id)).Select(package => new OrderApplyPostResponseDetail()
                        {
                            PackageId = package.Id,
                            PackageName = package.Name,
                        }).ToList();
                    }
                }
            }
            #endregion
            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("申请出货");

            //标题
            int currentRow = 0;
            IRow headRow = sheet.CreateRow(currentRow);
            headRow.CreateCell(0).SetCellValue("用户代码");
            headRow.CreateCell(1).SetCellValue("发货渠道");
            headRow.CreateCell(2).SetCellValue("目的国家");
            headRow.CreateCell(3).SetCellValue("包裹数量");
            headRow.CreateCell(4).SetCellValue("内单号");
            headRow.CreateCell(5).SetCellValue("申请时间");
            headRow.CreateCell(6).SetCellValue("订单状态");
            headRow.CreateCell(7).SetCellValue("经手人");
            headRow.CreateCell(8).SetCellValue("出货时间");
            currentRow++;
            foreach (var item in orderApplyResponseList.List)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.AppuserCode);
                row.CreateCell(1).SetCellValue(item.Channelname);
                row.CreateCell(2).SetCellValue(item.Nationname);
                row.CreateCell(3).SetCellValue(item.Details.Count);
                row.CreateCell(4).SetCellValue(item.Code);
                row.CreateCell(5).SetCellValue(item.Addtime.ToString());
                row.CreateCell(6).SetCellValue(Enum.GetName(typeof(OrderApplyStatus), item.Orderstatus));
                row.CreateCell(7).SetCellValue(item.Packageusername);
                row.CreateCell(8).SetCellValue(item.Packagetime.ToString());
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


        public class OrderApplyRequest
        {
            public long Id { get; set; }
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public long Currencychangerateid { get; set; }
            public decimal? Targetprice { get; set; }
            public IList<OrderApplyRequestdetail> Details { get; set; }
        }
        public class OrderApplyRequestdetail
        {
            public int? Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Customprice { get; set; }
            public decimal? Sueprice { get; set; }
            public decimal? Overlengthprice { get; set; }
            public decimal? Overweightprice { get; set; }
            public decimal? Oversizeprice { get; set; }
            public decimal? Paperprice { get; set; }
            public decimal? Boxprice { get; set; }
            public decimal? Bounceprice { get; set; }
            public decimal? Vacuumprice { get; set; }
            public decimal? Price { get; set; }
            public decimal? Targetprice { get; set; }
            public decimal? Length { get; set; }
            public decimal? Width { get; set; }
            public decimal? Height { get; set; }
            public decimal? Volumeweight { get; set; }
            public decimal? RemotePrice { get; set; }
            public bool? Haselectrified { get; set; }
            public decimal? PackAddPrice { get; set; }

            public List<long> Packages { get; set; }
        }
        public class OrderApplyResponse
        {
            public string Code { get; set; }
            public long Id { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public decimal Channelprice { get; set; }
            public List<ChannelResponsepricedetail> Pricedetails { get; set; }

            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public long Appuser { get; set; }
            public string AppuserCode { get; set; }
            public string Appusername { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public int Status { get; set; }
            public long Packageuser { get; set; }
            public string Packageusername { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime? Packagetime { get; set; }

            public List<OrderApplyResponseDetail> Details { get; set; }
            public OrderApplyPostResponse PostedDetails { get; set; }
        }
        public class OrderApplyPostResponse
        {
            public string Remark { get; set; }
            public string Mailcode { get; set; }
            public decimal? Price { get; set; }
            public IList<OrderApplyPostResponsedetail> Details { get; set; }
        }
        public class OrderApplyPostResponsedetail
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
            public List<OrderApplyPostResponseDetail> Packages { get; set; }
        }
        public class OrderApplyPostResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageName { get; set; }
        }

        public class OrderApplyResponseDetail
        {
            public long PackageId { get; set; }
            public string PackageCode { get; set; }
            public string PackageName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Payedprice { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
        }

        public class OrderApplyResponseList
        {
            public int Count { get; set; }
            public IList<OrderApplyResponse> List { get; set; }
            public long? Currencychangerateid { get; set; }
            public decimal? Currencychangerate { get; set; }

        }

        public class ChannelResponsepricedetail
        {
            public long Id { get; set; }
            public long Channelid { get; set; }
            public long Nationid { get; set; }
            public decimal Minweight { get; set; }
            public decimal Maxweight { get; set; }
            public decimal Price { get; set; }
        }

    }
}
