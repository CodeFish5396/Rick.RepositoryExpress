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
    /// 国家导入
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NationImportController : RickControllerBase
    {
        private readonly ILogger<NationImportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IChannelService _channelService;
        private readonly string filePath = "../Uploads/Temp/";
        private readonly RedisClientService _redisClientService;

        public NationImportController(ILogger<NationImportController> logger, IChannelService channelService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _channelService = channelService;
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
        /// 国家导入
        /// </summary>
        /// <param name="fileUploadRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<RickWebResult<List<List<string>>>> Post([FromForm] ChannelpriceUploadRequest fileUploadRequest)
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
            string ext = fileUploadRequest.Name.Substring(fileUploadRequest.Name.LastIndexOf("."));
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
                for (int i = 0; i <= 1; i++)
                {
                    tableDatas[0].Add(headRow.GetCell(i).ToString());
                }

                List<string> nationNames = new List<string>();
                List<string> nationCodes = new List<string>();
                int lastRow = sheet.LastRowNum;

                for (int i = 1; i <= lastRow; i++)
                {
                    List<string> rowList = new List<string>();
                    for (int j = 0; j <= 1; j++)
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
                                        nationNames.Add(ijValue);

                                    }
                                    if (j == 0)
                                    {
                                        nationCodes.Add(ijValue);

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
                    if (tableDatas[0][0] != "国家编码")
                    {
                        hasError = true;
                        tableDatas[0][0] += "!! 国家编码";
                    }
                    if (tableDatas[0][1] != "国家名称")
                    {
                        hasError = true;
                        tableDatas[0][1] += "!! 国家名称";
                    }
                }
                nationNames = nationNames.Distinct().ToList();
                nationCodes = nationCodes.Distinct().ToList();

                var nations = await _channelService.QueryAsync<Nation>(t => t.Status == 1 && (nationNames.Contains(t.Name) || nationCodes.Contains(t.Code)));

                for (int i = 1; i < tableDatas.Count; i++)
                {
                    if (string.IsNullOrEmpty(tableDatas[i][0]))
                    {
                        hasError = true;
                        tableDatas[i][0] += "!! 不能为空";
                    }
                    else if (nations.Any(t => t.Code == tableDatas[i][0]))
                    {
                        hasError = true;
                        tableDatas[i][0] += "!! 重复的国家编码";
                    }

                    if (string.IsNullOrEmpty(tableDatas[i][1]))
                    {
                        hasError = true;
                        tableDatas[i][1] += "!! 不能为空";
                    }
                    else if (nations.Any(t => t.Name == tableDatas[i][1]))
                    {
                        hasError = true;
                        tableDatas[i][1] += "!! 重复的国家名称";
                    }
                }

                if (!hasError)
                {
                    //记录到数据库
                    await _channelService.BeginTransactionAsync();

                    //2、遍历每一行，构建Channelprice
                    for (int i = 1; i < tableDatas.Count; i++)
                    {
                        Nation nation = new Nation();
                        nation.Id = _idGenerator.NextId();
                        nation.Name = tableDatas[i][1];
                        nation.Code = tableDatas[i][0];
                        nation.Status = 1;
                        nation.Addtime = now;
                        nation.Lasttime = now;
                        nation.Adduser = UserInfo.Id;
                        nation.Lastuser = UserInfo.Id;
                        await _channelService.AddAsync(nation);
                    }
                    await _channelService.CommitAsync();
                }

            }
            return RickWebResult.Success(tableDatas);
        }

        public class ChannelpriceUploadRequest
        {
            public string Name { get; set; }
            public List<IFormFile> Files { get; set; }
        }

    }
}
