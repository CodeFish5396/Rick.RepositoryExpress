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
    public class AppnewController : RickControllerBase
    {
        private readonly ILogger<AppnewController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppnewService _appnewService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/";

        public AppnewController(ILogger<AppnewController> logger, IAppnewService appnewService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
                filePath = dr + "\\Uploads\\";
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
        public async Task<RickWebResult<AppnewGetResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from appnew in _appnewService.Query<Appnew>(t => t.Status == 1)
                        select new AppnewGetResponse() { 
                            Id = appnew.Id,
                            Title = appnew.Title,
                            Type = appnew.Type,
                            Vicetitle = appnew.Vicetitle,
                            Imageid = appnew.Imageid,
                            Urlid = appnew.Urlid,
                            Isshow = appnew.Isshow == 1,
                            Status = appnew.Status,
                            Addtime = appnew.Addtime
                        };
            AppnewGetResponseList appnewGetResponseList = new AppnewGetResponseList();
            appnewGetResponseList.Count = await query.CountAsync();

            appnewGetResponseList.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(appnewGetResponseList);
        }


        /// <summary>
        /// 提交新闻
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] AppnewPostRequest appnewPostRequest)
        {
            await _appnewService.BeginTransactionAsync();

            //将Base64转Byte[]。然后保存
            byte[] content = Convert.FromBase64String(appnewPostRequest.Content);

            DateTime now = DateTime.Now;
            Fileinfo htmlfileinfo = new Fileinfo();
            htmlfileinfo.Id = _idGenerator.NextId();
            htmlfileinfo.Ext = ".html";
            htmlfileinfo.Mime = "text/html";
            htmlfileinfo.Status = 1;
            htmlfileinfo.Addtime = now;
            htmlfileinfo.Adduser = UserInfo.Id;
            htmlfileinfo.Filename = Guid.NewGuid().ToString("N");
            htmlfileinfo.Name = htmlfileinfo.Filename + htmlfileinfo.Ext;

            using (var fileStream = new FileStream(filePath + htmlfileinfo.Filename + htmlfileinfo.Ext, FileMode.Create))
            {
                await fileStream.WriteAsync(content);
            }

            Appnew appnew = new Appnew();
            appnew.Id = _idGenerator.NextId();
            appnew.Type = appnewPostRequest.Type;
            appnew.Title = appnewPostRequest.Title;
            appnew.Vicetitle = appnewPostRequest.Vicetitle;
            appnew.Urlid = htmlfileinfo.Id;
            appnew.Imageid = appnewPostRequest.Imageid;
            appnew.Status = 1;
            appnew.Addtime = now;
            appnew.Adduser = UserInfo.Id;
            appnew.Lasttime = now;
            appnew.Lastuser = UserInfo.Id;
            appnew.Isshow = appnewPostRequest.Isshow ? 1 : 0;
            await _appnewService.AddAsync(htmlfileinfo);
            await _appnewService.AddAsync(appnew);
            await _appnewService.CommitAsync();

            return RickWebResult.Success<object>(new object());
        }
        
        /// <summary>
        /// 修改新闻
        /// </summary>
        /// <param name="appnewPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] AppnewPutRequest  appnewPutRequest)
        {
            await _appnewService.BeginTransactionAsync();

            Appnew appnew = await _appnewService.FindAsync<Appnew>(appnewPutRequest.Id);

            //将Base64转Byte[]。然后保存
            byte[] content = Convert.FromBase64String(appnewPutRequest.Content);

            DateTime now = DateTime.Now;
            Fileinfo htmlfileinfo = new Fileinfo();
            htmlfileinfo.Id = _idGenerator.NextId();
            htmlfileinfo.Ext = ".html";
            htmlfileinfo.Mime = "text/html";
            htmlfileinfo.Status = 1;
            htmlfileinfo.Addtime = now;
            htmlfileinfo.Adduser = UserInfo.Id;
            htmlfileinfo.Filename = Guid.NewGuid().ToString("N");
            htmlfileinfo.Name = htmlfileinfo.Filename + htmlfileinfo.Ext;

            using (var fileStream = new FileStream(filePath + htmlfileinfo.Filename + htmlfileinfo.Ext, FileMode.Create))
            {
                await fileStream.WriteAsync(content);
            }

            appnew.Type = appnewPutRequest.Type;
            appnew.Title = appnewPutRequest.Title;
            appnew.Vicetitle = appnewPutRequest.Vicetitle;
            appnew.Urlid = htmlfileinfo.Id;
            appnew.Imageid = appnewPutRequest.Imageid;
            appnew.Status = 1;
            appnew.Lasttime = now;
            appnew.Lastuser = UserInfo.Id;
            appnew.Isshow = appnewPutRequest.Isshow ? 1 : 0;
            await _appnewService.AddAsync(htmlfileinfo);
            await _appnewService.UpdateAsync(appnew);
            await _appnewService.CommitAsync();

            return RickWebResult.Success<object>(new object());
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            DateTime now = DateTime.Now;

            Appnew appnew = await _appnewService.FindAsync<Appnew>(id);
            appnew.Status = 0;
            appnew.Lasttime = now;
            appnew.Lastuser = UserInfo.Id;

            await _appnewService.UpdateAsync(appnew);
            return RickWebResult.Success(new object());
        }
        public class AppnewPostRequest
        {
            public string Title { get; set; }
            public int Type { get; set; }
            public string Vicetitle { get; set; }
            public long Imageid { get; set; }
            public string Content { get; set; }
            public bool Isshow { get; set; }
        }

        public class AppnewPutRequest
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public int Type { get; set; }
            public string Vicetitle { get; set; }
            public long Imageid { get; set; }
            public string Content { get; set; }
            public bool Isshow { get; set; }

        }

        public class AppnewGetResponseList
        {
            public int Count { get; set; }
            public List<AppnewGetResponse> List { get; set; }

        }


        public class AppnewGetResponse
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public int Type { get; set; }
            public string Vicetitle { get; set; }
            public long Imageid { get; set; }
            public long Urlid { get; set; }
            public bool Isshow { get; set; }

            public int Status { get; set; }
            public DateTime Addtime { get; set; }
        }

    }
}
