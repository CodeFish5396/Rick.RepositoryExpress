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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppnewcontentController : RickControllerBase
    {
        private readonly ILogger<AppnewcontentController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppnewService _appnewService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/";

        public AppnewcontentController(ILogger<AppnewcontentController> logger, IAppnewService appnewService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appnewService = appnewService;
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
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\";
            }
            else
            {
                filePath = dr + "/Uploads/";
            }
        }

        /// <summary>
        /// 获取新闻列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppnewcontentGetResponse>> Get([FromQuery] long id)
        {
            Appnew appnew = await _appnewService.FindAsync<Appnew>(id);
            AppnewcontentGetResponse appnewGetResponse = new AppnewcontentGetResponse();
            appnewGetResponse.Id = appnew.Id;
            appnewGetResponse.Title = appnew.Title;
            appnewGetResponse.Type = appnew.Type;
            appnewGetResponse.Vicetitle = appnew.Vicetitle;
            appnewGetResponse.Imageid = appnew.Imageid;
            appnewGetResponse.Content = string.Empty;
            appnewGetResponse.Source = appnew.Source;
            appnewGetResponse.Urlid = appnew.Urlid;
            if (appnewGetResponse.Source == 0 || appnewGetResponse.Source == 1)
            {
                Fileinfo fileinfo = await _appnewService.FindAsync<Fileinfo>(appnew.Urlid);
                FileInfo fileInfo = new FileInfo(filePath + fileinfo.Filename + fileinfo.Ext);

                byte[] fileByte = new byte[fileInfo.Length];
                using (FileStream fs = fileInfo.OpenRead())
                {
                    await fs.ReadAsync(fileByte, 0, fileByte.Length);
                }
                appnewGetResponse.Content = Convert.ToBase64String(fileByte);

            }
            appnewGetResponse.Isshow = appnew.Isshow == 1;
            return RickWebResult.Success(appnewGetResponse);
        }

        public class AppnewcontentGetResponse
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public int Type { get; set; }
            public int Source { get; set; }
            public long Urlid { get; set; }
            public string Vicetitle { get; set; }
            public long Imageid { get; set; }
            public string Content { get; set; }
            public bool Isshow { get; set; }
        }
    }
}
