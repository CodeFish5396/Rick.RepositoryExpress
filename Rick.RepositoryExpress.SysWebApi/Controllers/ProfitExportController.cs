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
    /// 收支核算
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProfitExportController : RickControllerBase
    {
        private readonly ILogger<ProfitController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAccountsubjectService _accountsubjectService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public ProfitExportController(ILogger<ProfitController> logger, IAccountsubjectService accountsubjectService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _accountsubjectService = accountsubjectService;
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
        /// 导出收支核算
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
      

            var query = from account in _accountsubjectService.Query<Account>(t => (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime <= endTime))
                        select new ProfitResponse()
                        {
                            Id = account.Id,
                            Currencyid = account.Currencyid,
                            Amount = account.Amount,
                            Adduser = account.Adduser,
                            Addtime = account.Addtime,
                            SubjectCode = account.Subjectcode,
                            Direction = account.Direction

                        };

            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();


            var sumQuery = await (from agentfeeresponse in query
                                  group new { agentfeeresponse.Amount, agentfeeresponse.Direction } by new { agentfeeresponse.Currencyid, agentfeeresponse.Direction }
                                  into sumItem
                                  select new ProfitResponseSum()
                                  {
                                      Currencyid = sumItem.Key.Currencyid,
                                      Direction = sumItem.Key.Direction,
                                      TotalAmount = sumItem.Sum(t => t.Amount)
                                  }).ToListAsync();

            var sumList = sumQuery;

            List<long> sumcurrencyids = sumList.Select(t => t.Currencyid).Distinct().ToList();
            List<Currency> sumcurrencies = await (from c in _accountsubjectService.Query<Currency>(t => sumcurrencyids.Contains(t.Id)) select c).ToListAsync();
            foreach (var incomeResponse in sumList)
            {
                var ccurrency = sumcurrencies.SingleOrDefault(t => t.Id == incomeResponse.Currencyid);
                incomeResponse.Currencyname = ccurrency.Name;
            }
            var InList = sumList.Where(t => t.Direction == 1).ToList();
            var OutList = sumList.Where(t => t.Direction == -1).ToList();
            var ProfitList = (from profit in sumList
                                             group new { profit.TotalAmount, profit.Direction } by new { profit.Currencyid, profit.Currencyname }
                                            into profitGT
                                             select new ProfitResponseSum()
                                             {
                                                 Currencyid = profitGT.Key.Currencyid,
                                                 Currencyname = profitGT.Key.Currencyname,
                                                 Direction = 0,
                                                 TotalAmount = profitGT.Sum(t=>t.TotalAmount * t.Direction)
                                             }).ToList();
            //收入
            var incomeAccountIds = list.Where(t => t.SubjectCode == "100").Select(t => t.Id).ToList();
            var appusers = await (from charge in _accountsubjectService.Query<Appuseraccountcharge>(t => incomeAccountIds.Contains(t.Accountid))
                           join appuser in _accountsubjectService.Query<Appuser>()
                           on charge.Appuser equals appuser.Id
                           select new {
                               Chargeid = charge.Id,
                               Accountid = charge.Accountid,
                               Appuserid = appuser.Id,
                               Appusername = appuser.Truename,
                               Appusercode = appuser.Usercode
                           }).ToListAsync();

            //代理商成本
            var agentFeeAccountIds = list.Where(t => t.SubjectCode == "200").Select(t => t.Id).ToList();
            var agents = await (from agentfee in _accountsubjectService.Query<Agentfee>(t => agentFeeAccountIds.Contains(t.Accountid))
                         join agent in _accountsubjectService.Query<Agent>()
                         on agentfee.Agentid equals agent.Id
                         select new {
                             Agentfeeid = agentfee.Id,
                             Accountid = agentfee.Accountid,
                             Agentid = agent.Id,
                             Agentname = agent.Name
                         }).ToListAsync();

            //运营成本
            var runFeeAccountIds = list.Where(t => t.SubjectCode == "300").Select(t => t.Id).ToList();
            var runFees = await _accountsubjectService.QueryAsync<Runfee>(t => runFeeAccountIds.Contains(t.Accountid));

            var sysUserids = list.Select(t => t.Adduser).ToList();
            var sysUsers = await _accountsubjectService.QueryAsync<Sysuser>(t => sysUserids.Contains(t.Id));

            foreach (ProfitResponse profitResponse in list)
            {
                profitResponse.Currencyname = sumcurrencies.SingleOrDefault(t => t.Id == profitResponse.Currencyid).Name;
                profitResponse.Addusername = sysUsers.SingleOrDefault(t => t.Id == profitResponse.Adduser).Name;
                switch (profitResponse.SubjectCode)
                {
                    case "100":
                        profitResponse.SubjectName = "收入";
                        profitResponse.Description = appusers.SingleOrDefault(t => t.Accountid == profitResponse.Id).Appusercode;
                        break;
                    case "200":
                        profitResponse.SubjectName = "代理商成本";
                        profitResponse.Description = agents.SingleOrDefault(t => t.Accountid == profitResponse.Id).Agentname;
                        break;
                    case "300":
                        profitResponse.SubjectName = "运营成本";
                        profitResponse.Description = runFees.SingleOrDefault(t => t.Accountid == profitResponse.Id).Name;
                        break;
                }
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("收支核算");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("货币类型");
            head.CreateCell(1).SetCellValue("货币金额");
            head.CreateCell(2).SetCellValue("经手人");
            head.CreateCell(3).SetCellValue("添加时间");
            head.CreateCell(4).SetCellValue("科目");
            head.CreateCell(5).SetCellValue("项目");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Currencyname);
                row.CreateCell(1).SetCellValue(item.Amount.ToString());
                row.CreateCell(2).SetCellValue(item.Addusername);
                row.CreateCell(3).SetCellValue(item.Addtime.ToString());
                row.CreateCell(4).SetCellValue(item.SubjectName);
                row.CreateCell(5).SetCellValue(item.Description.ToString());
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

        public class ProfitResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public int Direction { get; set; }
            public string Addusername { get; set; }
            public DateTime Addtime { get; set; }
            public string SubjectName { get; set; }
            public string SubjectCode { get; set; }
            public string Description { get; set; }

        }

        public class ProfitResponseSum
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public int Direction { get; set; }

            public decimal TotalAmount { get; set; }
        }

    }
}
