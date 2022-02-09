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
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 代理商交易查询
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentUserDataExportController : RickControllerBase
    {
        private readonly ILogger<AgentUserDataExportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentService _agentService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";

        public AgentUserDataExportController(ILogger<AgentUserDataExportController> logger, IAgentService agentService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentService = agentService;
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
                filePath = directory.GetDirectories().Where(t=>t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
                filePath = dr + "/Uploads/Temp/";
            }

        }

        /// <summary>
        /// 代理商交易导出报表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get([FromQuery] long id)
        {
            //查询用户余额、充值、交易情况
            AgentDataExportResponseList appUserResponseList = new AgentDataExportResponseList();

            #region 数据查询
            Agent agent = await _agentService.FindAsync<Agent>(id);

            appUserResponseList.Id = agent.Id;
            appUserResponseList.Name = agent.Name;

            var query = from viewagentuserdatum in _agentService.Query<Viewagentuserdatum>(t => t.Agentid == agent.Id)
                        select new AgentDataExportResponse()
                        {
                            Id = viewagentuserdatum.Id,
                            Type = viewagentuserdatum.Type,
                            Currencyid = viewagentuserdatum.Currencyid,
                            Amount = viewagentuserdatum.Amount,
                            Adduser = viewagentuserdatum.Adduser,
                            Addtime = viewagentuserdatum.Addtime,
                            Orderid = viewagentuserdatum.Orderid,
                            Ordercode = string.Empty,
                            Paytype = viewagentuserdatum.Paytype
                        };
            appUserResponseList.Count = await query.CountAsync();
            appUserResponseList.List = await query.OrderByDescending(t => t.Id).ToListAsync();

            var sumQuery = from viewagentuserdatum in query
                           group new { viewagentuserdatum.Type, viewagentuserdatum.Amount } by viewagentuserdatum.Currencyid
               into viewappuserdatumGT
                           select new AgentDataExportAccountResponse()
                           {
                               Currencyid = viewappuserdatumGT.Key,
                               Chargeamount = viewappuserdatumGT.Where(t => t.Type == 1).Sum(t => t.Amount),
                               Consumeamount = viewappuserdatumGT.Where(t => t.Type == -1).Sum(t => t.Amount)
                           };

            appUserResponseList.Accounts = await sumQuery.ToListAsync();

            var orderids = appUserResponseList.List.Where(t => t.Orderid > 0).Select(t => t.Orderid).ToList();
            if (orderids != null && orderids.Count > 0)
            {
                var orders = await _agentService.QueryAsync<Packageorderapply>(order => orderids.Contains(order.Id));
                foreach (AgentDataExportResponse appUserDataResponse in appUserResponseList.List)
                {
                    if (appUserDataResponse.Orderid > 0)
                    {
                        appUserDataResponse.Ordercode = orders.Single(t => t.Id == appUserDataResponse.Orderid).Code;
                    }
                }
            }

            var currencyIds = appUserResponseList.List.Select(t => t.Currencyid).ToList();
            currencyIds.AddRange(appUserResponseList.Accounts.Select(t => t.Currencyid));

            var currencies = await _agentService.QueryAsync<Currency>(c => currencyIds.Contains(c.Id));
            foreach (AgentDataExportResponse appUserDataResponse in appUserResponseList.List)
            {
                appUserDataResponse.Currencyname = currencies.Single(t => t.Id == appUserDataResponse.Currencyid).Name;
            }
            foreach (AgentDataExportAccountResponse appUserDataAccountResponse in appUserResponseList.Accounts)
            {
                appUserDataAccountResponse.Currencyname = currencies.Single(t => t.Id == appUserDataAccountResponse.Currencyid).Name;
            }
            #endregion

            //将appUserResponseList保存到EXCEL中
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("出货成本");

            //标题
            IRow headRow = sheet.CreateRow(0);
            headRow.CreateCell(0);
            headRow.CreateCell(1).SetCellValue("出货成本");
            headRow.CreateCell(2);
            headRow.CreateCell(3);
            headRow.CreateCell(4);
            headRow.CreateCell(5);
            headRow.CreateCell(6);
            headRow.CreateCell(7);
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0,0,1,7));

            IRow agentInfoRow = sheet.CreateRow(1);
            agentInfoRow.CreateCell(0);
            agentInfoRow.CreateCell(1).SetCellValue("代理商名称:");
            agentInfoRow.CreateCell(2).SetCellValue(appUserResponseList.Name);

            IRow emptyRow2 = sheet.CreateRow(2);

            int sumBegin = 3;
            int currentRow = 4;
            IRow sumBeginRow = sheet.CreateRow(sumBegin);
            sumBeginRow.CreateCell(0);
            sumBeginRow.CreateCell(1).SetCellValue("合计:");

            foreach (AgentDataExportAccountResponse agentDataExportAccountResponse in appUserResponseList.Accounts)
            {
                IRow sumRow = sheet.CreateRow(currentRow);
                sumRow.CreateCell(0);
                sumRow.CreateCell(1).SetCellValue(agentDataExportAccountResponse.Currencyname + ":");
                sumRow.CreateCell(2).SetCellValue("充值:");
                sumRow.CreateCell(3).SetCellValue(agentDataExportAccountResponse.Chargeamount.ToString());
                sumRow.CreateCell(4);
                sumRow.CreateCell(5).SetCellValue("支出:");
                sumRow.CreateCell(6).SetCellValue(agentDataExportAccountResponse.Consumeamount.ToString());
                currentRow++;
            }
            IRow emptyRowsumEnd = sheet.CreateRow(currentRow);
            currentRow++;
            IRow emptyRowsumTableHead = sheet.CreateRow(currentRow);
            emptyRowsumTableHead.CreateCell(0);

            emptyRowsumTableHead.CreateCell(1).SetCellValue("类型");
            emptyRowsumTableHead.CreateCell(2).SetCellValue("金额");
            emptyRowsumTableHead.CreateCell(3).SetCellValue("经手人");
            emptyRowsumTableHead.CreateCell(4).SetCellValue("时间");
            emptyRowsumTableHead.CreateCell(5).SetCellValue("充值方式");
            emptyRowsumTableHead.CreateCell(6).SetCellValue("支出对应订单");
            emptyRowsumTableHead.CreateCell(7);
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(currentRow, currentRow, 6, 7));
            currentRow++;
            foreach (AgentDataExportResponse agentDataExportResponse in appUserResponseList.List)
            {
                IRow dataRow = sheet.CreateRow(currentRow);
                dataRow.CreateCell(0);
                dataRow.CreateCell(1).SetCellValue(agentDataExportResponse.Type == -1 ? "支出":"充值");
                dataRow.CreateCell(2).SetCellValue(agentDataExportResponse.Amount.ToString());
                dataRow.CreateCell(3).SetCellValue(agentDataExportResponse.Adduser);
                dataRow.CreateCell(4).SetCellValue(agentDataExportResponse.Addtime);
                dataRow.CreateCell(5).SetCellValue(agentDataExportResponse.Paytype);
                dataRow.CreateCell(6).SetCellValue(agentDataExportResponse.Ordercode);
                dataRow.CreateCell(7);
                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(currentRow, currentRow, 6, 7));
                currentRow++;
            }
            string fileName = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                book.Write(fileStream);
            }

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fileStream.Length));
                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }
        }

        public class AgentDataExportResponse
        {
            public long Id { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }

            public long Type { get; set; }
            public decimal Amount { get; set; }
            public long Adduser { get; set; }
            public DateTime Addtime { get; set; }
            public long Paytype { get; set; }
            public long Orderid { get; set; }
            public string Ordercode { get; set; }
        }

        public class AgentDataExportAccountResponse
        {
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public decimal Chargeamount { get; set; }
            public decimal Consumeamount { get; set; }
        }

        public class AgentDataExportResponseList
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<AgentDataExportAccountResponse> Accounts { get; set; }
            public int Count { get; set; }
            public List<AgentDataExportResponse> List { get; set; }
        }

    }
}
