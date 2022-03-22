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
<<<<<<< HEAD
    /// 快递标签导出
=======
    /// 标签导出
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TagExportController : RickControllerBase
    {
        private readonly ILogger<TagExportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
<<<<<<< HEAD
        private readonly string demofilePath = "../Uploads/TagDemo/";
=======
        private readonly string demofilePath = "../Uploads/ShippmentDemo/";
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
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
<<<<<<< HEAD
                demofilePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\TagDemo\\";
=======
                demofilePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\ShippmentDemo\\";
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
<<<<<<< HEAD
                demofilePath = dr + "/Uploads/TagDemo/";
=======
                demofilePath = dr + "/Uploads/ShippmentDemo/";
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
                filePath = dr + "/Uploads/Temp/";
            }
        }

        /// <summary>
<<<<<<< HEAD
        /// 快递标签导出
=======
        /// 出货面单导出
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
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

<<<<<<< HEAD
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
=======
            ShippmentResponse shippmentResponse = new ShippmentResponse();

            var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();

            shippmentResponse.Id = packageorderapply.Id;
            shippmentResponse.Sendcompany = "达人集运";
            shippmentResponse.Sendname = "田志波";
            shippmentResponse.Sendmobile = repository.Recivermobil;
            shippmentResponse.Sendaddress = repository.Region + repository.Address;

            shippmentResponse.Recievername = appuseraddress.Name;
            shippmentResponse.Recievercompany = string.Empty;
            shippmentResponse.Recievermobile = appuseraddress.Contactnumber;
            shippmentResponse.Recievercode = string.Empty;
            shippmentResponse.Recievercountryname = nation.Name;
            shippmentResponse.Recieveraddress = appuseraddress.Region + appuseraddress.Address;
            shippmentResponse.Boxcount = (int)packageorderapplyexpress.Totalcount;
            shippmentResponse.Boxweight = (decimal)packageorderapplyexpress.Totalweight;
            shippmentResponse.Hasbattery = packageorderapplyexpressdetails.Any(t => t.Haselectrified == 1);

            shippmentResponse.Boxes = packageorderapplyexpressdetails.Select(t => new ShippmentBoxdetail() {
                Length = (decimal)t.Length,
                Width = (decimal)t.Width,
                Height = (decimal)t.Height,
                Weight = (decimal)t.Weight,
                Volumeweight = (decimal)t.Volumeweight
            }).ToList();

            shippmentResponse.Ordercode = packageorderapply.Code;
            shippmentResponse.Appuserid = packageorderapply.Appuser;
            shippmentResponse.Appusercode = appuser.Usercode;
            shippmentResponse.Appusername = appuser.Name;
            shippmentResponse.Channelid = packageorderapply.Channelid;
            shippmentResponse.Channlename = channel.Name;
            shippmentResponse.Remoteprice = packageorderapplyexpressdetails.Sum(t => t.Remoteprice ?? 0);
            shippmentResponse.Remark = packageorderapply.Remark;
            shippmentResponse.CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
            //2 复制模板
            string demoFile = demofilePath + courier.Code + "ShippmentDemo.xlsx";
            string tempFile = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            XSSFWorkbook book ;
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
            //3 填写模板
            using (var fileStream = new FileStream(demoFile, FileMode.Open, FileAccess.Read))
            {
                book = new XSSFWorkbook(fileStream);
<<<<<<< HEAD
=======

>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
            }
            #region 写入Sheet
            ISheet sheet = book.GetSheetAt(0);

<<<<<<< HEAD
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
=======
            //发货人信息
            //公司名
            ICell cell41 = sheet.GetRow(4).GetCell(1);
            cell41.SetCellValue(cell41.StringCellValue + shippmentResponse.Sendcompany);
            //发件人
            ICell cell43 = sheet.GetRow(4).GetCell(3);
            cell43.SetCellValue(cell43.StringCellValue + shippmentResponse.Sendname);
            //电话
            ICell cell61 = sheet.GetRow(6).GetCell(1);
            cell61.SetCellValue(cell61.StringCellValue + shippmentResponse.Sendmobile);
            //地址
            ICell cell71 = sheet.GetRow(7).GetCell(1);
            cell71.SetCellValue(cell71.StringCellValue + shippmentResponse.Sendaddress);

            //收件人信息
            //公司名
            ICell cell111 = sheet.GetRow(11).GetCell(1);
            cell111.SetCellValue(cell111.StringCellValue + shippmentResponse.Recievercompany);
            //收件人
            ICell cell113 = sheet.GetRow(11).GetCell(3);
            cell113.SetCellValue(cell113.StringCellValue + shippmentResponse.Recievername);
            //电话
            ICell cell131 = sheet.GetRow(13).GetCell(1);
            cell131.SetCellValue(cell131.StringCellValue + shippmentResponse.Recievermobile);
            //地址
            ICell cell141 = sheet.GetRow(14).GetCell(1);
            cell141.SetCellValue(cell141.StringCellValue + shippmentResponse.Recieveraddress);
            //邮编
            ICell cell181 = sheet.GetRow(18).GetCell(1);
            cell181.SetCellValue(cell181.StringCellValue + shippmentResponse.Recievercode);
            //州名
            ICell cell182 = sheet.GetRow(18).GetCell(2);
            cell182.SetCellValue(cell182.StringCellValue + shippmentResponse.Recieverregion);
            //国家
            ICell cell183 = sheet.GetRow(18).GetCell(3);
            cell183.SetCellValue(cell183.StringCellValue + shippmentResponse.Recievercountryname);

            //货物信息
            //件数
            ICell cell85 = sheet.GetRow(8).GetCell(5);
            cell85.SetCellValue(shippmentResponse.Boxcount);
            //重量
            ICell cell86 = sheet.GetRow(8).GetCell(6);
            cell86.SetCellValue((double)shippmentResponse.Boxweight);
            //是否带电
            ICell cell87 = sheet.GetRow(8).GetCell(7);
            cell87.SetCellValue(shippmentResponse.Hasbattery?"是":"否");
            //打印日期
            ICell cell185 = sheet.GetRow(18).GetCell(5);
            cell185.SetCellValue(shippmentResponse.CurrentDate);

            int startIndex = 8;
            int endIndex = 18;

            int calEndIndex = startIndex + shippmentResponse.Boxes.Count - 1;
            endIndex = calEndIndex <= endIndex ? calEndIndex : endIndex;
            int currentIndex = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                ICell cell1i8 = sheet.GetRow(i).GetCell(9);
                cell1i8.SetCellValue(currentIndex + 1);

                ICell cell1i9 = sheet.GetRow(i).GetCell(10);
                cell1i9.SetCellValue(shippmentResponse.Boxes[currentIndex].Length + "*" + shippmentResponse.Boxes[currentIndex].Width + "*" + shippmentResponse.Boxes[currentIndex].Height);

                ICell cell1i12 = sheet.GetRow(i).GetCell(13);
                cell1i12.SetCellValue((double)shippmentResponse.Boxes[currentIndex].Volumeweight);
                currentIndex++;
            }
            //单号
            ICell cell219 = sheet.GetRow(21).GetCell(10);
            cell219.SetCellValue(shippmentResponse.Ordercode);
            //用户名
            ICell cell222 = sheet.GetRow(22).GetCell(2);
            cell222.SetCellValue(shippmentResponse.Appusername);
            //客户代码
            ICell cell226 = sheet.GetRow(22).GetCell(6);
            cell226.SetCellValue(shippmentResponse.Appusercode);
            //日期
            ICell cell229 = sheet.GetRow(22).GetCell(9);
            cell229.SetCellValue(shippmentResponse.CurrentDate);
            //BarCodeHelper.SaveBarCode(shippmentResponse.Ordercode.Substring(2), filePath + Guid.NewGuid().ToString("N") + ".jpg");
            //内单条码 TO-DO  
            byte[] bcImage = BarCodeHelper.GetBarCode(shippmentResponse.Ordercode.Substring(2));
            int pictureIdx = book.AddPicture(bcImage, PictureType.JPEG);
            IDrawing patriarch = sheet.CreateDrawingPatriarch();
            // 插图片的位置  HSSFClientAnchor（dx1,dy1,dx2,dy2,col1,row1,col2,row2) 后面再作解释
            IClientAnchor anchor = patriarch.CreateAnchor(0, 0, 0, 0, 2, 23, 5, 24);
            //把图片插到相应的位置
            IPicture pict = patriarch.CreatePicture(anchor, pictureIdx);

            //ICell cell232 = sheet.GetRow(23).GetCell(2);
            //cell232.SetCellValue(shippmentResponse.Ordercode);
            //发货渠道
            ICell cell236 = sheet.GetRow(23).GetCell(6);
            cell236.SetCellValue(shippmentResponse.Channlename);
            //邮编
            ICell cell242 = sheet.GetRow(24).GetCell(2);
            cell242.SetCellValue(shippmentResponse.Recievercode);

            //偏远费
            ICell cell246 = sheet.GetRow(24).GetCell(6);
            cell246.SetCellValue((double)shippmentResponse.Remoteprice);

            //备注
            ICell cell256 = sheet.GetRow(25).GetCell(6);
            cell256.SetCellValue(shippmentResponse.Remark);
            //件数
            ICell cell272 = sheet.GetRow(27).GetCell(2);
            cell272.SetCellValue(shippmentResponse.Boxcount);
            //重量
            ICell cell274 = sheet.GetRow(27).GetCell(4);
            cell274.SetCellValue((double)shippmentResponse.Boxweight);
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064

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

<<<<<<< HEAD
        public class TagResponse
=======
        public class ShippmentResponse
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
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
<<<<<<< HEAD
            public int Boxcount { get; set; }
            public decimal Boxweight { get; set; }
            public bool Hasbattery { get; set; }
=======

            public int Boxcount { get; set; }
            public decimal Boxweight { get; set; }
            public bool Hasbattery { get; set; }

            public List<ShippmentBoxdetail> Boxes { get; set; }
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064
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
<<<<<<< HEAD
=======
        public class ShippmentBoxdetail
        {
            public decimal Length { get; set; }
            public decimal Width { get; set; }
            public decimal Height { get; set; }
            public decimal Weight { get; set; }
            public decimal Volumeweight { get; set; }
        }
>>>>>>> b357048c5e60a3594dee53e3c601cad807a56064


    }
}
