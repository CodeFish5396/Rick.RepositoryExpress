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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : RickControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFileService _fileService;
        private readonly string filePath = "F:\\Uploads\\";

        public FileController(ILogger<FileController> logger, IFileService fileService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _fileService = fileService;
            _idGenerator = idGenerator;
        }

        /// <summary>
        /// 文件Url
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get([FromQuery]long id)
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
                byte[] buffer = new byte[fi.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                FileContentResult fileContentResult = new FileContentResult(buffer, fileinfo.Mime);
                return fileContentResult;
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileUploadRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<RickWebResult<FileUploadResult>> Post([FromForm] FileUploadRequest fileUploadRequest)
        {
            await _fileService.BeginTransactionAsync();

            FileUploadResult fileUploadResult = new FileUploadResult();
            if (fileUploadRequest.Files.Count == 0)
            {
                return RickWebResult.Error(new FileUploadResult(), 909, ConstString.FileNo);
            }
            else if (fileUploadRequest.Files.Count > 1)
            {
                return RickWebResult.Error(new FileUploadResult(), 909, ConstString.FileLimit);
            }
            IFormFile file = fileUploadRequest.Files[0];
            Fileinfo fileinfo = new Fileinfo();
            fileinfo.Id = _idGenerator.NextId();
            fileinfo.Name = fileUploadRequest.Name ?? file.FileName;
            fileinfo.Ext = fileUploadRequest.Name.Substring(fileUploadRequest.Name.LastIndexOf("."));
            fileinfo.Mime = file.ContentType;
            fileinfo.Status = 1;
            DateTime now = DateTime.Now;
            fileinfo.Addtime = now;
            fileinfo.Adduser = UserInfo.Id;
            fileinfo.Filename = Guid.NewGuid().ToString("N");
            using (var fileStream = new FileStream(filePath + fileinfo.Filename + fileinfo.Ext, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            await _fileService.AddAsync(fileinfo);
            await _fileService.CommitAsync();
            fileUploadResult.Id = fileinfo.Id;
            return RickWebResult.Success(fileUploadResult);
        }
        public class FileUploadResult
        {
            public long Id { get; set; }
        }
        public class FileUploadRequest
        {
            public string Name { get; set; }
            public List<IFormFile> Files { get; set; }
        }
    }
}
