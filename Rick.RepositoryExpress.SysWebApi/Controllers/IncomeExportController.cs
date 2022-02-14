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
    public class IncomeExportController : RickControllerBase
    {
        private readonly ILogger<IncomeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IIncomeService _incomeService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public IncomeExportController(ILogger<IncomeController> logger, IIncomeService incomeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _incomeService = incomeService;
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
        /// 导出用户充值xlsx
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="currencyId"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] long? currencyId)
        {
            

            var query = from charge in _incomeService.Query<Appuseraccountcharge>(t => t.Status == 1 && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime) && (!currencyId.HasValue || t.Currencyid == currencyId))
                        join account in _incomeService.Query<Account>()
                        on charge.Accountid equals account.Id
                        select new IncomeResponse()
                        {
                            Id = charge.Id,
                            Appuser = charge.Appuser,
                            Amount = charge.Amount,
                            Currencyid = charge.Currencyid,
                            Addtime = charge.Addtime,
                            Adduser = account.Adduser,
                            Paytype = charge.Paytype
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by agentfeeresponse.Currencyid
                           into sumItem
                                  select new IncomeResponseSum()
                                  {
                                      Currencyid = sumItem.Key,
                                      TotalAmount = sumItem.Sum()
                                  }).ToListAsync();

            var sumList = sumQuery;
            List<long> sumcurrencyids = sumList.Select(t => t.Currencyid).Distinct().ToList();
            List<Currency> sumcurrencies = await (from c in _incomeService.Query<Currency>(t => sumcurrencyids.Contains(t.Id)) select c).ToListAsync();
            foreach (var incomeResponse in sumList)
            {
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.Currencyname = ccurrency.Name;
            }

            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();

            List<long> users = list.Select(t => t.Appuser).Distinct().ToList();
            //List<long> currencyids = incomeRequestList.List.Select(t => t.Currencyid).Distinct().ToList();
            List<Appuser> appusers = await (from c in _incomeService.Query<Appuser>(t => users.Contains(t.Id)) select c).ToListAsync();
            //List<Currency> currencies = await (from c in _incomeService.Query<Currency>(t => currencyids.Contains(t.Id)) select c).ToListAsync();

            List<long> sysuserids = list.Select(t => t.Adduser).Distinct().ToList();
            List<Sysuser> sysusers = await (from c in _incomeService.Query<Sysuser>(t => sysuserids.Contains(t.Id)) select c).ToListAsync();

            foreach (var incomeResponse in list)
            {
                var cuser = appusers.SingleOrDefault(t => t.Id == incomeResponse.Appuser);
                incomeResponse.UserName = cuser.Name;
                incomeResponse.UserMobil = cuser.Mobile;
                incomeResponse.Usercode = cuser.Usercode;
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.CurrencyName = ccurrency.Name;
                var csysuser = sysusers.SingleOrDefault(t => t.Id == incomeResponse.Adduser);
                incomeResponse.Addusername = csysuser.Name;
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("收入");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("用户名");
            head.CreateCell(1).SetCellValue("用户代码");
            head.CreateCell(2).SetCellValue("用户手机");
            head.CreateCell(3).SetCellValue("币种");
            head.CreateCell(4).SetCellValue("充值方式");
            head.CreateCell(5).SetCellValue("金额");
            head.CreateCell(6).SetCellValue("经手人");
            head.CreateCell(7).SetCellValue("充值时间");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.UserName);
                row.CreateCell(1).SetCellValue(item.Usercode);
                row.CreateCell(2).SetCellValue(item.UserMobil);
                row.CreateCell(3).SetCellValue(item.CurrencyName);
                row.CreateCell(4).SetCellValue(Enum.GetName(typeof(PayType), item.Paytype));
                row.CreateCell(5).SetCellValue(item.Amount.ToString());
                row.CreateCell(6).SetCellValue(item.Addusername);
                row.CreateCell(7).SetCellValue(item.Addtime.ToString());
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
        public class IncomeResponse
        {
            public long Id { get; set; }
            public long Appuser { get; set; }
            public string UserName { get; set; }
            public string UserMobil { get; set; }
            public string Usercode { get; set; }
            public decimal Amount { get; set; }
            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public DateTime Addtime { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public int Paytype { get; set; }

        }


        public class IncomeResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }


    }
}
