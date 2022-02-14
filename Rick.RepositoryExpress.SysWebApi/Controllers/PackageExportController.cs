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
    public class PackageExportController : RickControllerBase
    {
        private readonly ILogger<PackageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public PackageExportController(ILogger<PackageController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
        /// 导出库存查询xlsx
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="expressNumber"></param>
        /// <param name="addUser"></param>
        /// <param name="location"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] string expressNumber, [FromQuery] string addUser, [FromQuery] string location)
        {

            var query = from package in _packageService.Query<Package>(t => (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        && (!status.HasValue || t.Status == status)
                        && (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                        && (string.IsNullOrEmpty(location) || t.Location.Contains(location))
                        )
                        join sysuser in _packageService.Query<Sysuser>(t => string.IsNullOrEmpty(addUser) || t.Name == addUser)
                        on package.Adduser equals sysuser.Id
                        join courier in _packageService.Query<Courier>()
                        on package.Courierid equals courier.Id
                        into courierTmp
                        from courier in courierTmp.DefaultIfEmpty()
                        join repository in _packageService.Query<Repository>()
                        on package.Repositoryid equals repository.Id
                        into repositoryTmp
                        from repository in repositoryTmp.DefaultIfEmpty()
                        select new PackageResponse()
                        {
                            Id = package.Id,
                            Repositoryid = package.Repositoryid,
                            RepositoryName = repository == null ? string.Empty : repository.Name,
                            Courierid = package.Courierid,
                            CourierName = courier == null ? string.Empty : courier.Name,
                            Expressnumber = package.Expressnumber,
                            Location = package.Location,
                            Name = package.Name,
                            Remark = package.Remark,
                            Status = package.Status,
                            Adduser = package.Adduser,
                            Addusername = sysuser.Name,
                            Addtime = package.Addtime,
                        };

            var list = await query.OrderByDescending(t => t.Id).ToListAsync();
            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("包裹上架");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("快递单号");
            head.CreateCell(1).SetCellValue("快递名称");
            head.CreateCell(2).SetCellValue("品名");
            head.CreateCell(3).SetCellValue("仓库名称");
            head.CreateCell(4).SetCellValue("经手人");
            head.CreateCell(5).SetCellValue("位置");
            head.CreateCell(6).SetCellValue("入库时间");
            head.CreateCell(7).SetCellValue("状态");
            head.CreateCell(8).SetCellValue("备注");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Expressnumber);
                row.CreateCell(1).SetCellValue(item.CourierName);
                row.CreateCell(2).SetCellValue(item.Name);
                row.CreateCell(3).SetCellValue(item.RepositoryName);
                row.CreateCell(4).SetCellValue(item.Addusername);
                row.CreateCell(5).SetCellValue(item.Location.ToString());
                row.CreateCell(6).SetCellValue(item.Addtime.ToString());
                row.CreateCell(7).SetCellValue(Enum.GetName(typeof(PackageStatus), item.Status));
                row.CreateCell(8).SetCellValue(item.Remark);
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

        public class PackageResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string RepositoryName { get; set; }
            public long Courierid { get; set; }
            public string CourierName { get; set; }
            public string Expressnumber { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public string Remark { get; set; }
            public int Status { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public List<long> Images { get; set; }
            public List<long> Videos { get; set; }


        }


    }
}
