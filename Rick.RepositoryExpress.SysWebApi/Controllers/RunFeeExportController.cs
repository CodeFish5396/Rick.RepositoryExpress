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
    /// 代理商成本
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RunFeeExportController : RickControllerBase
    {
        private readonly ILogger<RunFeeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRunFeeService _runFeeService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        /// <summary>
        /// 运营成本成本
        /// </summary>
        private string accountSubjectId = "1475363978350825472";
        private string accountSubjectCode = "300";

        public RunFeeExportController(ILogger<RunFeeController> logger, IRunFeeService runFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _runFeeService = runFeeService;
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
        /// 导出代理商成本xlsx
        /// </summary>
        /// <param name="currencyid"></param>
        /// <param name="name"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] long? currencyid, [FromQuery] string name, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {


            var query = from account in _runFeeService.Query<Account>(t => t.Subjectcode == accountSubjectCode && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        join runfee in _runFeeService.Query<Runfee>(t => (string.IsNullOrEmpty(name) || t.Operator == name))
                        on account.Id equals runfee.Accountid
                        join currency in _runFeeService.Query<Currency>(t => !currencyid.HasValue || t.Id == currencyid)
                        on account.Currencyid equals currency.Id
                        join user in _runFeeService.Query<Sysuser>()
                        on account.Adduser equals user.Id
                        select new RunFeeResponse()
                        {
                            Id = account.Id,
                            Name = runfee.Name,
                            Currencyid = account.Currencyid,
                            Currencyname = currency.Name,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addusername = user.Name,
                            Addtime = account.Addtime,
                            Paytime = runfee.Paytime,
                            Operator = runfee.Operator
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by new { agentfeeresponse.Currencyid, agentfeeresponse.Currencyname }
                                  into sumItem
                                  select new RunFeeRequestSum()
                                  {
                                      Currencyid = sumItem.Key.Currencyid,
                                      Currencyname = sumItem.Key.Currencyname,
                                      TotalAmount = sumItem.Sum()
                                  }).ToListAsync();

            var SumList = sumQuery;

       
            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("运营成本");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("费用项目");
            head.CreateCell(1).SetCellValue("货币名称");
            head.CreateCell(2).SetCellValue("货币金额");
            head.CreateCell(3).SetCellValue("经手人");
            head.CreateCell(4).SetCellValue("发生时间");
            head.CreateCell(5).SetCellValue("添加时间");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Name);
                row.CreateCell(1).SetCellValue(item.Currencyname);
                row.CreateCell(2).SetCellValue(item.Amount.ToString());
                row.CreateCell(3).SetCellValue(item.Operator);
                row.CreateCell(4).SetCellValue(item.Paytime.ToString());
                row.CreateCell(5).SetCellValue(item.Addtime.ToString());
                currentRow++;
            }


            string fileName = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                book.Write(fileStream);
            }
            book.Close();

            #endregion
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fileStream.Length));

                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }
        }

        public class RunFeeResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }

            public string Operator { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public DateTime Paytime { get; set; }
        }

        public class RunFeeRequestSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }

        public class RunFeeRequest
        {
            public string Name { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public DateTime Paytime { get; set; }

        }

    }
}
