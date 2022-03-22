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
using NPOI.HSSF.UserModel;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 快递标签导出
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TagExportController : RickControllerBase
    {
        private readonly ILogger<TagExportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        private readonly string demofilePath = "../Uploads/TagDemo/";
        private readonly string filePath = "../Uploads/Temp/";

        public TagExportController(ILogger<TagExportController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
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
                demofilePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\TagDemo\\";
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
                demofilePath = dr + "/Uploads/TagDemo/";
                filePath = dr + "/Uploads/Temp/";
            }
        }

        /// <summary>
        /// 快递标签导出
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get([FromQuery] long id)
        {
            //1 查询订单
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;
            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(id);
            Packageorderapplyexpress packageorderapplyexpress = (await _packageOrderApplyService.QueryAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)).Single();
            Appuseraddress appuseraddress = await _packageOrderApplyService.FindAsync<Appuseraddress>(packageorderapply.Addressid);
            Repository repository = (await _packageOrderApplyService.QueryAsync<Repository>(t => t.Status == 1)).First();
            Nation nation = await _packageOrderApplyService.FindAsync<Nation>(appuseraddress.Nationid);
            Appuser appuser = await _packageOrderApplyService.FindAsync<Appuser>(packageorderapply.Appuser);
            Channel channel = await _packageOrderApplyService.FindAsync<Channel>(packageorderapply.Channelid);
            Courier courier = await _packageOrderApplyService.FindAsync<Courier>((long)packageorderapplyexpress.Courierid);

            TagResponse tagResponse = new TagResponse();

            var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();

            tagResponse.Id = packageorderapply.Id;
            tagResponse.Sendcompany = "达人集运";
            tagResponse.Sendname = "田志波";
            tagResponse.Sendmobile = repository.Recivermobil;
            tagResponse.Sendaddress = repository.Region + repository.Address;

            tagResponse.Recievername = appuseraddress.Name;
            tagResponse.Recievercompany = string.Empty;
            tagResponse.Recievermobile = appuseraddress.Contactnumber;
            tagResponse.Recievercode = string.Empty;
            tagResponse.Recievercountryname = nation.Name;
            tagResponse.Recieveraddress = appuseraddress.Region + appuseraddress.Address;
            tagResponse.Boxcount = (int)packageorderapplyexpress.Totalcount;
            tagResponse.Boxweight = (decimal)packageorderapplyexpress.Totalweight;
            tagResponse.Hasbattery = packageorderapplyexpressdetails.Any(t => t.Haselectrified == 1);

            tagResponse.Ordercode = packageorderapply.Code;
            tagResponse.Appuserid = packageorderapply.Appuser;
            tagResponse.Appusercode = appuser.Usercode;
            tagResponse.Appusername = appuser.Name;
            tagResponse.Channelid = packageorderapply.Channelid;
            tagResponse.Channlename = channel.Name;
            tagResponse.Remoteprice = packageorderapplyexpressdetails.Sum(t => t.Remoteprice ?? 0);
            tagResponse.Remark = packageorderapply.Remark;
            tagResponse.CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
            //2 复制模板
            string demoFile = demofilePath + courier.Code + "Demo.xlsx";
            string tempFile = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            XSSFWorkbook book;
            //3 填写模板
            using (var fileStream = new FileStream(demoFile, FileMode.Open, FileAccess.Read))
            {
                book = new XSSFWorkbook(fileStream);
            }
            #region 写入Sheet
            ISheet sheet = book.GetSheetAt(0);

            //收货人
            ICell cell62 = sheet.GetRow(6).GetCell(2);
            cell62.SetCellValue(tagResponse.Recievername);

            //详细地址
            ICell cell71 = sheet.GetRow(7).GetCell(1);
            cell71.SetCellValue(tagResponse.Recieveraddress);

            //手机号
            ICell cell112 = sheet.GetRow(11).GetCell(2);
            cell112.SetCellValue(tagResponse.Recievermobile);

            //国家
            ICell cell122 = sheet.GetRow(12).GetCell(2);
            cell122.SetCellValue(tagResponse.Recievercountryname);

            //内单号
            ICell cell131 = sheet.GetRow(13).GetCell(1);
            cell131.SetCellValue(tagResponse.Ordercode);

            //内单条码 
            byte[] bcImage = BarCodeHelper.GetBarCode(tagResponse.Ordercode.Substring(2));
            int pictureIdx = book.AddPicture(bcImage, PictureType.JPEG);
            IDrawing patriarch = sheet.CreateDrawingPatriarch();
            // 插图片的位置  HSSFClientAnchor（dx1,dy1,dx2,dy2,col1,row1,col2,row2) 后面再作解释
            IClientAnchor anchor = patriarch.CreateAnchor(0, 0, 0, 0, 1, 14, 7, 16);
            //把图片插到相应的位置
            IPicture pict = patriarch.CreatePicture(anchor, pictureIdx);

            //渠道
            ICell cell161 = sheet.GetRow(16).GetCell(1);
            cell161.SetCellValue("渠道:" + tagResponse.Channlename);

            //客户编码
            ICell cell171 = sheet.GetRow(17).GetCell(1);
            cell171.SetCellValue("客户内部编码:" + tagResponse.Appusercode);

            #endregion
            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            {
                book.Write(fileStream);
            }
            book.Close();
            //4 下载模板
            using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fileStream.Length));

                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }
        }

        public class TagResponse
        {
            public long Id { get; set; }
            public string Sendcompany { get; set; }
            public string Sendname { get; set; }
            public string Sendmobile { get; set; }
            public string Sendaddress { get; set; }
            public string Recievername { get; set; }
            public string Recievercompany { get; set; }
            public string Recievermobile { get; set; }
            public string Recievercode { get; set; }
            public string Recieverregion { get; set; }
            public string Recievercountryname { get; set; }
            public string Recieveraddress { get; set; }
            public int Boxcount { get; set; }
            public decimal Boxweight { get; set; }
            public bool Hasbattery { get; set; }
            public string Ordercode { get; set; }
            public long Appuserid { get; set; }
            public string Appusercode { get; set; }
            public string Appusername { get; set; }
            public long Channelid { get; set; }
            public string Channlename { get; set; }
            public decimal Remoteprice { get; set; }
            public string Remark { get; set; }
            public string CurrentDate { get; set; }

        }


    }
}
