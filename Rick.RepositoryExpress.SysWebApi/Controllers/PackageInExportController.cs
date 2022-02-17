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
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 入库录单，录入货物信息
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PackageInExportController : RickControllerBase
    {
        private readonly ILogger<PackageInPreController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public PackageInExportController(ILogger<PackageInPreController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageService = packageService;
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
        /// 导出查询列表
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="expressNumber"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] string expressNumber, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {

            var baseQuery = from package in _packageService.Query<Package>(t => t.Status == (int)PackageStatus.已入库 && (string.IsNullOrEmpty(code) || t.Code == code) && (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber) && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
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
                                Code = package.Code,
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
                                Lasttime = package.Lasttime,
                                Status = package.Status,
                                Freightprice = package.Freightprice
                            };
            var currencies = await _packageService.QueryAsync<Currency>(t => t.Status == 1 && (t.Isdefault == 1 || t.Islocal == 1));
            var localCurrency = currencies.Single(t => t.Islocal == 1);
            var defaultCurrency = currencies.Single(t => t.Isdefault == 1);

            var list = await baseQuery.OrderByDescending(t => t.Addtime).ToListAsync();

            IEnumerable<long> ids = list.Select(t => t.Packageid);

            var imageInfos = await (from image in _packageService.Query<Packageimage>(t => ids.Contains(t.Packageid))
                                    select image
                                    ).ToListAsync();
            var vedioInfos = await (from vedio in _packageService.Query<Packagevideo>(t => ids.Contains(t.Packageid))
                                    select vedio
                        ).ToListAsync();

            foreach (var packageInResponse in list)
            {
                packageInResponse.Images = imageInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
                packageInResponse.Videos = vedioInfos.Where(t => t.Packageid == packageInResponse.Packageid).Select(t => t.Fileinfoid).ToList();
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("包裹录单");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("包裹编号");
            head.CreateCell(1).SetCellValue("预报时间");
            head.CreateCell(2).SetCellValue("录单时间");
            head.CreateCell(3).SetCellValue("快递公司");
            head.CreateCell(4).SetCellValue("快递单号");
            head.CreateCell(5).SetCellValue("品名");
            head.CreateCell(6).SetCellValue("状态");
            head.CreateCell(7).SetCellValue("到付金额($)");
            head.CreateCell(8).SetCellValue("重量(kg)");
            head.CreateCell(9).SetCellValue("数量");
            head.CreateCell(10).SetCellValue("预报用户代码");

            currentRow++;

            foreach (var item in list)
            {
                var baseRowIndex = currentRow;
                IRow row = sheet.CreateRow(currentRow); currentRow++;


                row.CreateCell(0).SetCellValue(item.Code);
                row.CreateCell(1).SetCellValue(item.Claimtime.ToString());
                row.CreateCell(2).SetCellValue(item.Addtime.ToString());
                row.CreateCell(3).SetCellValue(item.Couriername);
                row.CreateCell(4).SetCellValue(item.Expressnumber);
                row.CreateCell(5).SetCellValue(item.Name);
                row.CreateCell(6).SetCellValue(Enum.GetName(typeof(PackageStatus), item.Status));
                row.CreateCell(7).SetCellValue(item.Freightprice.ToString());
                row.CreateCell(8).SetCellValue(item.Weight.ToString());
                row.CreateCell(9).SetCellValue(item.Count.ToString());
                row.CreateCell(10).SetCellValue(item.Usercode);
                
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

    }


}
