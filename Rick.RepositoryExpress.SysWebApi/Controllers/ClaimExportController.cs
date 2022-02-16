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
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimExportController : RickControllerBase
    {
        private readonly ILogger<ClaimController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IExpressclaimService _expressclaimService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public ClaimExportController(ILogger<ClaimController> logger, IExpressclaimService expressclaimService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _expressclaimService = expressclaimService;
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
        /// 导出查询用户包裹预报
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="expressNumber"></param>
        /// <param name="userCode"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] string expressNumber, [FromQuery] string userCode)
        {
            
            var mainQuery = from expressclaim in _expressclaimService.Query<Expressclaim>(t => (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime) && (!status.HasValue || t.Status == status))
                            join expressinfo in _expressclaimService.Query<Expressinfo>(t => string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                            on expressclaim.Expressinfoid equals expressinfo.Id
                            join appuser in _expressclaimService.Query<Appuser>(t => string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            on expressclaim.Appuser equals appuser.Id
                            select new ExpressclaimResponse()
                            {
                                Id = expressclaim.Id,
                                Expressinfoid = expressclaim.Expressinfoid,
                                Expressnumber = expressinfo.Expressnumber,
                                Courierid = expressinfo.Courierid,
                                Packageid = expressclaim.Packageid,
                                Repositoryid = expressclaim.Repositoryid,
                                Appuser = expressclaim.Appuser,
                                Usercode = appuser.Usercode,
                                Username = appuser.Name,
                                Remark = expressclaim.Remark,
                                Count = expressclaim.Count,
                                Status = expressclaim.Status,
                                Addtime = expressclaim.Addtime
                            };
            var list = await mainQuery.OrderByDescending(t => t.Addtime).ToListAsync();

            var courierids =list.Select(t => t.Courierid).Distinct();
            var couriers = await _expressclaimService.QueryAsync<Courier>(t => courierids.Contains(t.Id));

            var packageids = list.Select(t => t.Packageid).Distinct();
            var packages = await _expressclaimService.QueryAsync<Package>(t => packageids.Contains(t.Id));
            
            var expressclaimids =list.Select(t => t.Id).Distinct().ToList();
            var expressclaimdetails = await _expressclaimService.QueryAsync<Expressclaimdetail>(t => expressclaimids.Contains(t.Expressclaimid));
            
            var repositorieids = list.Select(t => t.Repositoryid).Distinct();
            var repositories = await _expressclaimService.QueryAsync<Repository>(t => repositorieids.Contains(t.Id));

            foreach (var expressclaimresponse in list)
            {
                var currentCourier = couriers.SingleOrDefault(t => t.Id == expressclaimresponse.Courierid);
                expressclaimresponse.Couriername = currentCourier == null ? string.Empty : currentCourier.Name;

                var currentexpressclaimdetails = expressclaimdetails.Where(t => t.Expressclaimid == expressclaimresponse.Id).ToList();
                expressclaimresponse.Packagename = (currentexpressclaimdetails == null || currentexpressclaimdetails.Count == 0) ? string.Empty : string.Join(',', currentexpressclaimdetails.Select(t => t.Name).ToArray());
                var currentPackage = packages.SingleOrDefault(t => t.Id == expressclaimresponse.Packageid);

                expressclaimresponse.Location = currentPackage == null ? string.Empty : currentPackage.Location;
                expressclaimresponse.Repositoryname = repositories.Single(t => t.Id == expressclaimresponse.Repositoryid).Name;
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("包裹预报");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("预报用户昵称");
            head.CreateCell(1).SetCellValue("预报用户代码");
            head.CreateCell(2).SetCellValue("仓库名称");
            head.CreateCell(3).SetCellValue("预报时间");
            head.CreateCell(4).SetCellValue("快递名称");
            head.CreateCell(5).SetCellValue("快递单号");
            head.CreateCell(6).SetCellValue("品名");
            head.CreateCell(7).SetCellValue("数量");
            head.CreateCell(8).SetCellValue("预报状态");
            head.CreateCell(9).SetCellValue("柜架位置");
            
            head.CreateCell(10).SetCellValue("备注");
            currentRow++;

            foreach (var item in list)
            {
                var baseRowIndex = currentRow;
                IRow row = sheet.CreateRow(currentRow); currentRow++;


                row.CreateCell(0).SetCellValue(item.Username);
                row.CreateCell(1).SetCellValue(item.Usercode);
                row.CreateCell(2).SetCellValue(item.Repositoryname);
                row.CreateCell(3).SetCellValue(item.Addtime);
                row.CreateCell(4).SetCellValue(item.Couriername);
                row.CreateCell(5).SetCellValue(item.Expressnumber);
                row.CreateCell(6).SetCellValue(item.Packagename);

                row.CreateCell(7).SetCellValue(item.Count);
                row.CreateCell(8).SetCellValue(Enum.GetName(typeof(PackageNoteStatus), item.Status));
                row.CreateCell(9).SetCellValue(item.Location);
                row.CreateCell(10).SetCellValue(item.Remark);
                //sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(baseRowIndex, baseRowIndex + item.Profit.Count - 1, 6, 7));

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
        public class ExpressclaimResponse
        {
            public long Id { get; set; }
            public long Expressinfoid { get; set; }
            public string Expressnumber { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public long? Packageid { get; set; }
            public string Packagename { get; set; }
            public string Location { get; set; }
            public long Repositoryid { get; set; }
            public string Repositoryname { get; set; }
            public long Appuser { get; set; }
            public string Usercode { get; set; }
            public string Username { get; set; }
            public string Remark { get; set; }
            public int Count { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
        }



    }
}
