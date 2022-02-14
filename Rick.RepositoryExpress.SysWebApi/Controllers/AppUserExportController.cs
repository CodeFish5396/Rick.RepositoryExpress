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
    /// <summary>
    /// APP用户管理
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AppUserExportController : RickControllerBase
    {
        private readonly ILogger<AppUserController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";

        public AppUserExportController(ILogger<AppUserController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuserService = appuserService;
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
        /// 导出APP用户查询xlsx
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="countryName"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string countryName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status)
        {

            var query = from user in _appuserService.Query<Appuser>(t => (!status.HasValue || t.Status == status)
                        && (string.IsNullOrEmpty(userName) || t.Truename == userName)
                        && (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                        && (string.IsNullOrEmpty(countryName) || t.Countryname == countryName)
                        && (!startTime.HasValue || t.Addtime >= startTime)
                        && (!endTime.HasValue || t.Addtime <= endTime)
                        )
                        select new AppUserResponse()
                        {
                            Id = user.Id,
                            Mobile = user.Mobile,
                            Usercode = user.Usercode,
                            Countrycode = user.Countrycode,
                            Name = user.Truename,
                            Headportrait = user.Headportrait,
                            Addtime = user.Addtime,
                            Cityname = user.Cityname,
                            Gender = user.Gender,
                            Birthdate = user.Birthdate,
                            Email = user.Email,
                            Nickname = user.Name,
                            Countryname = user.Countryname,
                            Address = user.Address,
                            Status = user.Status
                        };
            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();
            var userIds = list.Select(t => t.Id);
            var accounts = await _appuserService.QueryAsync<Appuseraccount>(t => userIds.Contains(t.Appuser) && t.Status == 1);
            var currencyids = accounts.Select(t => t.Currencyid);
            var currencies = await _appuserService.QueryAsync<Currency>(t => currencyids.Contains(t.Id) && t.Status == 1);
            foreach (var appuserItem in list)
            {
                appuserItem.Accounts = (from account in accounts
                                        join currency in currencies
                                        on account.Currencyid equals currency.Id
                                        where account.Appuser == appuserItem.Id
                                        select new AppUserAccountResponse() { 
                                            Id = account.Id,
                                            Currencyid = account.Currencyid,
                                            Currencyname = currency.Name,
                                            Amount = account.Amount
                                        }).ToList();
            }
            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("APP用户管理");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("用户代码");
            head.CreateCell(1).SetCellValue("昵称");
            head.CreateCell(2).SetCellValue("姓名");
            head.CreateCell(3).SetCellValue("手机号");
            head.CreateCell(4).SetCellValue("邮箱");
            head.CreateCell(5).SetCellValue("性别");
            head.CreateCell(6).SetCellValue("生日");
            head.CreateCell(7).SetCellValue("国家编码");
            head.CreateCell(8).SetCellValue("所在国家");
            head.CreateCell(9).SetCellValue("所在城市");
            head.CreateCell(10).SetCellValue("详细地址");
            head.CreateCell(11).SetCellValue("注册时间");
            head.CreateCell(12).SetCellValue("账号余额");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Usercode);
                row.CreateCell(1).SetCellValue(item.Nickname);
                row.CreateCell(2).SetCellValue(item.Name);
                row.CreateCell(3).SetCellValue(item.Mobile);
                row.CreateCell(4).SetCellValue(item.Email);
                row.CreateCell(5).SetCellValue(item.Gender);
                row.CreateCell(6).SetCellValue(item.Birthdate.HasValue?item.Birthdate.ToString():"无");
                row.CreateCell(7).SetCellValue(item.Countrycode);
                row.CreateCell(8).SetCellValue(item.Countryname);
                row.CreateCell(9).SetCellValue(item.Cityname);
                row.CreateCell(10).SetCellValue(item.Address);
                row.CreateCell(11).SetCellValue(item.Addtime);
               row.CreateCell(12).SetCellValue(string.Join('/', item.Accounts.Select(t => t.Currencyname + ":" + t.Amount)));
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

        public class AppUserResponse
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Usercode { get; set; }
            public string Countrycode { get; set; }
            public string Countryname { get; set; }
            public string Name { get; set; }
            public string Nickname { get; set; }
            public string Headportrait { get; set; }
            public DateTime Addtime { get; set; }
            public string Cityname { get; set; }
            public string Gender { get; set; }
            public DateTime? Birthdate { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public int Status { get; set; }
            public List<AppUserAccountResponse> Accounts { get; set; }
        }
        public class AppUserAccountResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }

        }



    }
}
