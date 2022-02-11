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
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepositoryInExportController : RickControllerBase
    {
        private readonly ILogger<RepositoryInController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageService _packageService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public RepositoryInExportController(ILogger<RepositoryInController> logger, IPackageService packageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
        /// 导出包裹入库xlsx
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="expressNumber"></param>
        /// <param name="inUserName"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] string expressNumber, [FromQuery] string inUserName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
			#region 读取数据
			bool isUser = !(string.IsNullOrEmpty(userCode) && string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userMobile));
            List<long> packageids = new List<long>();
            if (isUser)
            {
                var appUsers = await (from appuser in _packageService.Query<Appuser>(t =>
                                (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                                && (string.IsNullOrEmpty(userName) || t.Name == userName)
                                && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                              )
                                      select appuser.Id).ToListAsync();
                packageids = await (from claim in _packageService.Query<Expressclaim>(t => appUsers.Contains(t.Appuser) && t.Packageid.HasValue)
                                    select (long)claim.Packageid).ToListAsync();
            }

            var query = from package in _packageService.Query<Package>(t => (string.IsNullOrEmpty(expressNumber) || t.Expressnumber == expressNumber)
                          && (!startTime.HasValue || t.Addtime >= startTime)
                          && (!endTime.HasValue || t.Addtime <= endTime)
                          && (string.IsNullOrEmpty(code) || t.Code == code)
                          && (t.Status >= (int)PackageStatus.已入库 && t.Status != (int)PackageStatus.已出库)
                          )
                        join courier in _packageService.Query<Courier>()
                        on package.Courierid equals courier.Id
                        join user in _packageService.Query<Sysuser>()
                        on package.Repositoryinuser equals user.Id
                        into userTemp
                        from user in userTemp.DefaultIfEmpty()
                        select new RepositoryInResponse()
                        {
                            Id = package.Id,
                            Repositoryid = package.Repositoryid,
                            Expressnumber = package.Expressnumber,
                            CourierId = courier.Id,
                            CourierName = courier.Name,
                            Code = package.Code,
                            Name = package.Name,
                            Weight = package.Weight,
                            Count = package.Count,
                            Inuser = package.Repositoryinuser,
                            Inusername = user == null ? string.Empty : user.Name,
                            Location = package.Location,
                            Addtime = package.Addtime,
                            Intime = package.Repositoryintime,
                            Status = package.Status,
                            Changecode = package.Changecode,
                            Refundcode = package.Refundcode,
                            Checkremark = package.Checkremark,
                            Refundremark = package.Refundremark,
                            Changeremark = package.Changeremark,
                            Repositoryregionid = package.Repositoryregionid,
                            Repositoryshelfid = package.Repositoryshelfid,
                            Repositorylayerid = package.Repositorylayerid,
                            Repositorynumber = package.Repositorynumber
                        };

            if (isUser)
            {
                query = from repositoryinresponse in query
                        where packageids.Contains(repositoryinresponse.Id)
                        select repositoryinresponse;
            }
            if (!string.IsNullOrEmpty(inUserName))
            {
                query = from repositoryinresponse in query
                        where repositoryinresponse.Inusername == inUserName
                        select repositoryinresponse;
            }
        
            var repositoryInResponceList =  await query.OrderByDescending(t => t.Id).ToListAsync();
            IEnumerable<long> ids = repositoryInResponceList.Select(t => t.Id);



            var users = await (from claim in _packageService.Query<Expressclaim>(t => t.Packageid.HasValue && ids.Contains((long)t.Packageid))
                               join user in _packageService.Query<Appuser>()
                               on claim.Appuser equals user.Id
                               select new
                               {
                                   claim.Packageid,
                                   user.Id,
                                   user.Usercode,
                                   user.Truename,
                                   user.Name,
                                   user.Mobile
                               }
                        ).ToListAsync();


            foreach (var packageInResponse in repositoryInResponceList)
            {
                packageInResponse.Users = users.Where(t => t.Packageid == packageInResponse.Id).Select(t => new RepositoryInUserInfoResponse()
                {
                    Userid = t.Id,
                    Usercode = t.Usercode,
                    Username = t.Truename,
                    Usermobile = t.Mobile
                }).ToList();
                
            }
			#endregion
			#region 写入Sheet
			XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("包裹上架");

            //标题
            int currentRow = 0;
            IRow emptyRowsumTableHead = sheet.CreateRow(currentRow);
            emptyRowsumTableHead.CreateCell(0).SetCellValue("用户代码");
            emptyRowsumTableHead.CreateCell(1).SetCellValue("录单时间");
            emptyRowsumTableHead.CreateCell(2).SetCellValue("快递单号");
            emptyRowsumTableHead.CreateCell(3).SetCellValue("包裹编号");
            emptyRowsumTableHead.CreateCell(4).SetCellValue("品名");
            emptyRowsumTableHead.CreateCell(5).SetCellValue("数量");
            emptyRowsumTableHead.CreateCell(6).SetCellValue("重量(KG)");
            emptyRowsumTableHead.CreateCell(7).SetCellValue("入柜时间");
            emptyRowsumTableHead.CreateCell(8).SetCellValue("柜架位置");
            emptyRowsumTableHead.CreateCell(9).SetCellValue("入柜经手人");
            emptyRowsumTableHead.CreateCell(10).SetCellValue("状态");
            currentRow++;
            
            foreach (var item in repositoryInResponceList)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Users!=null&&item.Users.Count>0?item.Users[0].Usercode: "————");
                row.CreateCell(1).SetCellValue(item.Addtime.ToString());
                row.CreateCell(2).SetCellValue(item.Expressnumber);
                row.CreateCell(3).SetCellValue(item.Code);
                row.CreateCell(4).SetCellValue(item.Name);
                row.CreateCell(5).SetCellValue(item.Count);
                row.CreateCell(6).SetCellValue(item.Weight.HasValue? ((float)item.Weight.Value):0);
                row.CreateCell(7).SetCellValue(item.Intime.HasValue?item.Intime.ToString():"无");
                row.CreateCell(8).SetCellValue(item.Location);
                row.CreateCell(9).SetCellValue(item.Inusername);
                row.CreateCell(10).SetCellValue(Enum.GetName(typeof(PackageStatus), item.Status));
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

       

        public class RepositoryInRequest
        {
            public long Id { get; set; }
            public long Repositoryregionid { get; set; }
            public long Repositoryshelfid { get; set; }
            public long Repositorylayerid { get; set; }
            public string Repositorynumber { get; set; }
        }

        public class RepositoryInResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }

            public string Expressnumber { get; set; }
            public string Code { get; set; }
            public long CourierId { get; set; }
            public string CourierName { get; set; }
            public string Name { get; set; }
            public decimal? Weight { get; set; }
            public int Count { get; set; }
            public long? Inuser { get; set; }
            public string Inusername { get; set; }
            public string Location { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime? Intime { get; set; }
            public int Status { get; set; }
            public string Changecode { get; set; }
            public string Refundcode { get; set; }

            public string Checkremark { get; set; }
            public string Refundremark { get; set; }
            public string Changeremark { get; set; }
            public long? Repositoryregionid { get; set; }
            public long? Repositoryshelfid { get; set; }
            public long? Repositorylayerid { get; set; }

            public string Repositorynumber { get; set; }

            public IList<long> Images { get; set; }
            public IList<long> Videos { get; set; }
            public IList<RepositoryInUserInfoResponse> Users { get; set; }
        }

        public class RepositoryInUserInfoResponse
        {
            public long Userid { get; set; }
            public string Usercode { get; set; }
            public string Username { get; set; }
            public string Usermobile { get; set; }
        }




    }
}
