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
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 运营费用导入
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RunfeeImportController : RickControllerBase
    {
        private readonly ILogger<RunfeeImportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRunFeeService _runFeeService;
        private readonly string filePath = "../Uploads/Runfee/";
        private readonly RedisClientService _redisClientService;
        private string accountSubjectCode = "300";

        public RunfeeImportController(ILogger<RunfeeImportController> logger, IRunFeeService runFeeService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Runfee\\";
            }
            else
            {
                filePath = dr + "/Uploads/Runfee/";
            }

        }

        /// <summary>
        /// 运营费用导入
        /// </summary>
        /// <param name="fileUploadRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<RickWebResult<List<List<string>>>> Post([FromForm] RunfeeImportUploadRequest fileUploadRequest)
        {
            List<List<string>> tableDatas = new List<List<string>>();

            if (fileUploadRequest.Files.Count == 0)
            {
                return RickWebResult.Error(tableDatas, 909, ConstString.FileNo);
            }
            else if (fileUploadRequest.Files.Count > 1)
            {
                return RickWebResult.Error(tableDatas, 909, ConstString.FileLimit);
            }
            IFormFile file = fileUploadRequest.Files[0];
            string fileName = Guid.NewGuid().ToString("N");
            string ext = ".xlsx";
            string tempFileFullPath = filePath + fileName + ext;
            using (var fileStream = new FileStream(tempFileFullPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            using (FileStream exlfileStream = new FileStream(tempFileFullPath, FileMode.Open))
            {
                XSSFWorkbook book = new XSSFWorkbook(exlfileStream);
                ISheet sheet = book.GetSheetAt(0);
                if (sheet.LastRowNum == 0 || sheet.LastRowNum == 1)
                {
                    return RickWebResult.Error(tableDatas, 996, "Excel列表数据项为空!");
                }
                DateTime now = DateTime.Now;
                //1，查找第一行，检查表头是否正确
                var headRow = sheet.GetRow(0);
                tableDatas.Add(new List<string>());
                for (int i = 0; i <= 4; i++)
                {
                    tableDatas[0].Add(headRow.GetCell(i).ToString());
                }
                List<string> currencyNames = new List<string>();
                int lastRow = sheet.LastRowNum;
                
                for (int i = 1; i < lastRow; i++)
                {
                    List<string> rowList = new List<string>();
                    for (int j = 0; j <= 4; j++)
                    {
                        string ijValue = string.Empty;
                        var row = sheet.GetRow(i);
                        if (row != null)
                        {
                            var cell = row.GetCell(j);
                            if (cell != null)
                            {
                                string cellValue = cell.ToString().Trim();
                                if (!string.IsNullOrEmpty(cellValue))
                                {
                                    ijValue = cellValue;
                                    if (j == 1)
                                    {
                                        currencyNames.Add(ijValue);
                                    }
                                }
                            }
                            rowList.Add(ijValue);
                        }
                    }

                    tableDatas.Add(rowList);
                }

                bool hasError = false;

                //2、检查第一行
                {
                    if (tableDatas[0][0] != "费用项目")
                    {
                        hasError = true;
                        tableDatas[0][0] += "!! 费用项目";
                    }
                    if (tableDatas[0][1] != "货币名称")
                    {
                        hasError = true;
                        tableDatas[0][1] += "!! 货币名称";
                    }
                    if (tableDatas[0][2] != "金额")
                    {
                        hasError = true;
                        tableDatas[0][2] += "!! 金额";
                    }
                    if (tableDatas[0][3] != "经手人")
                    {
                        hasError = true;
                        tableDatas[0][3] += "!! 经手人";
                    }
                    if (tableDatas[0][4] != "发生时间")
                    {
                        hasError = true;
                        tableDatas[0][4] += "!! 发生时间";
                    }

                }

                //3、检查每一行数据
                //系统中的货币
                var currencies = await _runFeeService.QueryAsync<Currency>(t => t.Status == 1 && currencyNames.Contains(t.Name));
                decimal checkD = 0;
                DateTime checkDate = DateTime.Now;
                for (int i = 1; i < tableDatas.Count; i++)
                {
                    if (string.IsNullOrEmpty(tableDatas[i][0]))
                    {
                        hasError = true;
                        tableDatas[i][0] += "!! 不能为空";
                    }

                    if (string.IsNullOrEmpty(tableDatas[i][1]))
                    {
                        hasError = true;
                        tableDatas[i][1] += "!! 不能为空";
                    }
                    else if (!currencies.Any(t=>t.Name == tableDatas[i][1]))
                    {
                        hasError = true;
                        tableDatas[i][1] += "!! 货币名称错误";
                    }

                    if (string.IsNullOrEmpty(tableDatas[i][2]))
                    {
                        hasError = true;
                        tableDatas[i][2] += "!! 不能为空";
                    }
                    else
                    {
                        if (!decimal.TryParse(tableDatas[i][2], out checkD))
                        {
                            hasError = true;
                            tableDatas[i][2] += "!! 数据格式不正确";
                        }
                    }

                    if (string.IsNullOrEmpty(tableDatas[i][3]))
                    {
                        hasError = true;
                        tableDatas[i][3] += "!! 不能为空";
                    }

                    if (string.IsNullOrEmpty(tableDatas[i][4]))
                    {
                        hasError = true;
                        tableDatas[i][4] += "!! 不能为空";
                    }
                    else
                    {
                        if (!DateTime.TryParse(tableDatas[i][4], out checkDate))
                        {
                            hasError = true;
                            tableDatas[i][4] += "!! 数据格式不正确";
                        }
                    }
                }

                if (!hasError)
                {
                    //记录到数据库
                    await _runFeeService.BeginTransactionAsync();

                    //2、遍历每一行，构建Channelprice
                    for (int i = 1; i < tableDatas.Count; i++)
                    {
                        Account account = new Account();
                        account.Id = _idGenerator.NextId();
                        account.Currencyid = currencies.Single(t=>t.Name == tableDatas[i][1]).Id;
                        account.Amount = Convert.ToDecimal(tableDatas[i][2]);
                        account.Status = 1;
                        account.Addtime = now;
                        account.Adduser = UserInfo.Id;
                        account.Subjectcode = accountSubjectCode;
                        account.Direction = -1;
                        await _runFeeService.AddAsync(account);

                        Runfee runfee = new Runfee();
                        runfee.Id = _idGenerator.NextId();
                        runfee.Name = tableDatas[i][0];
                        runfee.Paytime = Convert.ToDateTime(tableDatas[i][4]);
                        runfee.Operator = tableDatas[i][3];
                        runfee.Accountid = account.Id;
                        runfee.Status = 1;
                        runfee.Addtime = now;
                        runfee.Adduser = UserInfo.Id;
                        await _runFeeService.AddAsync(runfee);
                    }
                    await _runFeeService.CommitAsync();
                }
            }
            return RickWebResult.Success(tableDatas);
        }
        
        public class RunfeeImportUploadRequest
        {
            public List<IFormFile> Files { get; set; }
        }

    }
}
