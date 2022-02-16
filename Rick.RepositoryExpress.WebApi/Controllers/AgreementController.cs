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
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    /// <summary>
    /// 发货协议
    /// </summary>
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class AgreementController : RickControllerBase
    {
        private readonly ILogger<AgreementController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFileService _fileService;
        private readonly string filePath = "../Uploads/";

        public AgreementController(ILogger<AgreementController> logger, IFileService fileService, IIdGeneratorService idGenerator)
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
        /// 发货协议
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get()
        {
            Appnew appnew = await _fileService.Query<Appnew>(t => t.Type == 6).FirstOrDefaultAsync();
            if (appnew == null)
            {
                return null;
            }
            else
            {
                Fileinfo fileinfo = await _fileService.FindAsync<Fileinfo>(appnew.Urlid);
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

                        byte[] buffer = new byte[fi.Length];
                        await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                        results.AddRange(buffer);

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
}
