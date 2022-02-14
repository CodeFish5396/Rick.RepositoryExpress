using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class HtmlController : RickControllerBase
    {
        private readonly ILogger<HtmlController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFileService _fileService;
        private readonly string filePath = "../Uploads/";

        public HtmlController(ILogger<HtmlController> logger, IFileService fileService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _fileService = fileService;
            _idGenerator = idGenerator;
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
        /// 文件Url
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get(long id)
        {
            Fileinfo fileinfo = await _fileService.FindAsync<Fileinfo>(id);
            string path = filePath + fileinfo.Filename + fileinfo.Ext;
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
            {
                return null;
            }
            using (var fileStream = new FileStream(filePath + fileinfo.Filename + fileinfo.Ext, FileMode.Open))
            {
                List<byte> results = new List<byte>();
                if (fileinfo.Ext == ".html")
                {
                    string htmlBegin = "<!doctype html ><html lang=\"zh\"><head><meta charSet=\"utf-8\"/><title data-react-helmet=\"true\">达人集运 - 新闻</title><meta name='viewport' content='width=device-width,target-densitydpi=high-dpi,initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0, user-scalable=no'/></head><body>";
                    byte[] begin = System.Text.Encoding.UTF8.GetBytes(htmlBegin);
                    results.AddRange(begin);

                    byte[] buffer = new byte[fi.Length];
                    await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                    results.AddRange(buffer);


                    string htmlEnd = "</body></html>";
                    byte[] end = System.Text.Encoding.UTF8.GetBytes(htmlEnd);
                    results.AddRange(end);
                }
                else
                {
                    byte[] buffer = new byte[fi.Length];
                    await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                    results.AddRange(buffer);
                }

                FileContentResult fileContentResult = new FileContentResult(results.ToArray(), fileinfo.Mime);
                
                return fileContentResult;
            }
        }
    }
}
