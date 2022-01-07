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
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelpriceController : RickControllerBase
    {
        private readonly ILogger<ChannelpriceController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IChannelService _channelService;
        private readonly string filePath = "F:\\Uploads\\ChannelPrice\\";
        private readonly RedisClientService _redisClientService;

        public ChannelpriceController(ILogger<ChannelpriceController> logger, IChannelService channelService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _channelService = channelService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 阶梯价格文件上传
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
                await _channelService.BeginTransactionAsync();
                DateTime now = DateTime.Now;
                Channel channel = await _channelService.FindAsync<Channel>(fileUploadRequest.Id);

                var oldChannelPrices = await _channelService.QueryAsync<Channelprice>(t => t.Channelid == channel.Id);
                foreach (Channelprice cp in oldChannelPrices)
                {
                    await _channelService.DeleteAsync<Channelprice>(cp.Id);
                }

                //1，查找第一行，找出全部国家
                var headRow = sheet.GetRow(0);
                List<string> nationNames = new List<string>();
                int lastColumn = headRow.LastCellNum - 1;
                int lastRow = sheet.LastRowNum;
                
                for (int j = 2; j < headRow.LastCellNum; j++)
                {
                    var cell = headRow.GetCell(j);
                    if (cell != null)
                    {
                        string cellValue = cell.ToString().Trim();
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            nationNames.AddRange(cellValue.Split("，"));
                        }
                        else
                        {
                            lastColumn = j - 1;
                        }
                    }
                }
                for (int i = 0; i <= lastRow; i++)
                {
                    List<string> rowList = new List<string>();

                    for (int j = 0; j <= lastColumn; j++)
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
                                }
                            }
                            rowList.Add(ijValue);
                        }
                    }

                    tableDatas.Add(rowList);
                }

                var nations = await _channelService.QueryAsync<Nation>(t => t.Status == 1 && nationNames.Contains(t.Name));

                //2、遍历每一行，构建Channelprice
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    var cellWeight = row.GetCell(1);
                    if (cellWeight == null)
                    {
                        break;
                    }

                    string priceLimitDescription = cellWeight.ToString().Trim();
                    if (string.IsNullOrEmpty(priceLimitDescription))
                    {
                        break;
                    }
                    string[] weights = priceLimitDescription.Split("-");
                    decimal minWeight = Convert.ToDecimal(weights[0]);
                    decimal maxWeight = Convert.ToDecimal(weights[1]);
                    for (int j = 2; j <= lastColumn; j++)
                    {
                        var currentNationNames = headRow.GetCell(j).ToString().Trim();
                        string[] nationNameArray = currentNationNames.Split("，");
                        var currentNations = nations.Where(t => nationNameArray.Contains(t.Name)).ToList();
                        decimal price = Convert.ToDecimal(row.GetCell(j).ToString().Trim());
                        foreach (var nation in currentNations)
                        {
                            Channelprice channelprice = new Channelprice();
                            channelprice.Id = _idGenerator.NextId();
                            channelprice.Channelid = channel.Id;
                            channelprice.Minweight = minWeight;
                            channelprice.Maxweight = maxWeight;
                            channelprice.Nationid = nation.Id;
                            channelprice.Price = price;
                            channelprice.Status = 1;
                            channelprice.Addtime = now;
                            channelprice.Lasttime = now;
                            channelprice.Adduser = UserInfo.Id;
                            channelprice.Lastuser = UserInfo.Id;
                            await _channelService.AddAsync(channelprice);
                        }
                    }
                }
                await _channelService.CommitAsync();
                _redisClientService.HashSet("ChannelPriceData", channel.Id.ToString(), JsonConvert.SerializeObject(tableDatas));

            }
            return RickWebResult.Success(tableDatas);
        }

        /// <summary>
        /// 查询渠道价格
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<List<List<string>>>> Get([FromQuery]long id)
        {
            Channel channel = await _channelService.FindAsync<Channel>(id);
            var channelPrices = await _channelService.QueryAsync<Channelprice>(t => t.Channelid == channel.Id);
            string ChannelPriceData = _redisClientService.HashGet("ChannelPriceData", channel.Id.ToString());
            List<List<string>> result = new List<List<string>>();
            if (!string.IsNullOrEmpty(ChannelPriceData))
            {
                result = JsonConvert.DeserializeObject<List<List<string>>>(ChannelPriceData);

            }
            return RickWebResult.Success(result);

        }
        public class ChannelpriceUploadResult
        {
            public long Id { get; set; }
        }
        public class ChannelpriceUploadRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<IFormFile> Files { get; set; }
        }

    }
}
