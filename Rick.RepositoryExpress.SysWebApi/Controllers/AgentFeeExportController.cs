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
    public class AgentFeeExportController : RickControllerBase
    {
        private readonly ILogger<AgentFeeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentFeeService _agentFeeService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        /// <summary>
        /// 代理商成本会计科目
        /// </summary>
        private string accountSubjectId = "1475363832376463360";
        private string accountSubjectCode = "200";

        public AgentFeeExportController(ILogger<AgentFeeController> logger, IAgentFeeService agentFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentFeeService = agentFeeService;
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
        /// 导出代理商成本
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
            

            var query = from account in _agentFeeService.Query<Account>(t => t.Subjectcode == accountSubjectCode && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        join agentfee in _agentFeeService.Query<Agentfee>()
                        on account.Id equals agentfee.Accountid
                        join currency in _agentFeeService.Query<Currency>(t => !currencyId.HasValue || t.Id == currencyId)
                        on account.Currencyid equals currency.Id
                        join agent in _agentFeeService.Query<Agent>()
                        on agentfee.Agentid equals agent.Id
                        join user in _agentFeeService.Query<Sysuser>()
                        on account.Adduser equals user.Id
                        select new AgentFeeResponse()
                        {
                            Id = account.Id,
                            Agentid = agentfee.Agentid,
                            Agentname = agent.Name,
                            Currencyid = account.Currencyid,
                            Currencyname = currency.Name,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addusername = user.Name,
                            Addtime = account.Addtime,
                            Paytype = agentfee.Paytype,
                        };

            var sumQuery = await (from agentfeeresponse in query
                                  group agentfeeresponse.Amount by new { agentfeeresponse.Currencyid, agentfeeresponse.Currencyname }
                                  into sumItem
                                  select new AgentFeeResponseSum()
                                  {
                                      Currencyid = sumItem.Key.Currencyid,
                                      Currencyname = sumItem.Key.Currencyname,
                                      TotalAmount = sumItem.Sum()
                                  }).ToListAsync();

            var sumList = sumQuery;
            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();
            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("出货成本管理");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("代理商");
            head.CreateCell(1).SetCellValue("货币名称");
            head.CreateCell(2).SetCellValue("充值方式");
            head.CreateCell(3).SetCellValue("金额");
            head.CreateCell(4).SetCellValue("经手人");
            head.CreateCell(5).SetCellValue("添加时间");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Agentname);
                row.CreateCell(1).SetCellValue(item.Currencyname);
                row.CreateCell(2).SetCellValue(Enum.GetName(typeof(PayType), item.Paytype));
                row.CreateCell(3).SetCellValue(item.Amount.ToString());
                row.CreateCell(4).SetCellValue(item.Addusername);
                row.CreateCell(5).SetCellValue(item.Addtime.ToString());
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

        public class AgentFeeResponse
        {
            public long Id { get; set; }
            public long Agentid { get; set; }
            public string Agentname { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public int Paytype { get; set; }
        }

        public class AgentFeeResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public decimal TotalAmount { get; set; }
        }

        public class AgentFeeRequest
        {
            public long Agentid { get; set; }
            public long Currencyid { get; set; }
            public decimal Amount { get; set; }
            public int Paytype { get; set; }
        }
    }
}
