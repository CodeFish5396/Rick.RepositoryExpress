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
    /// 发票导出
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceExportController : RickControllerBase
    {
        private readonly ILogger<InvoiceExportController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        private readonly string demofilePath = "../Uploads/InvoiceDemo/";
        private readonly string filePath = "../Uploads/Temp/";

        public InvoiceExportController(ILogger<InvoiceExportController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
                demofilePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\InvoiceDemo\\";
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
                demofilePath = dr + "/Uploads/InvoiceDemo/";
                filePath = dr + "/Uploads/Temp/";
            }
        }

        /// <summary>
        /// 出货面单导出
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

            InvoiceResponse invoiceResponse = new InvoiceResponse();

            var packageorderapplyexpressdetails = await _packageOrderApplyService.Query<Packageorderapplyexpressdetail>(t => t.Packageorderapplyexpressid == packageorderapplyexpress.Id).ToListAsync();
            var packageQuery = from packageorderapplydetail in _packageOrderApplyService.Query<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status == 1)
                               join package in _packageOrderApplyService.Query<Package>()
                               on packageorderapplydetail.Packageid equals package.Id
                               join type1 in _packageOrderApplyService.Query<Goodtypel1>()
                               on package.Goodtypel1id equals type1.Id
                               join type2 in _packageOrderApplyService.Query<Goodtypel2>()
                               on package.Goodtypel2id equals type2.Id
                               select new InvoiceBoxdetail() {
                                   Packageid = package.Id,
                                   Packagename = package.Name,
                                   PackageCount = package.Count,
                                   Goodtype1id = type1.Id,
                                   Goodtype1name = type1.Name,
                                   Goodtype2id = type2.Id,
                                   Goodtype2name = type2.Name,
                                   Goodtype2code = type2.Code,
                                   Claimid = packageorderapplydetail.Exclaimid
                               };
            invoiceResponse.Id = packageorderapply.Id;
            invoiceResponse.Sendcompany = "达人集运";
            invoiceResponse.Sendname = "田志波";
            invoiceResponse.Sendcountry = "中国";
            invoiceResponse.Sendcode = "518000";
            invoiceResponse.Sendmobile = repository.Recivermobil;
            invoiceResponse.Sendaddress = repository.Region + repository.Address;
            invoiceResponse.Sendregion = repository.Region;
            invoiceResponse.Recievername = appuseraddress.Name;
            invoiceResponse.Recievercompany = string.Empty;
            invoiceResponse.Recievermobile = appuseraddress.Contactnumber;
            invoiceResponse.Recievercode = string.Empty;

            invoiceResponse.Recievercountryname = nation.Name;
            invoiceResponse.Recieveraddress = appuseraddress.Region + appuseraddress.Address;
            invoiceResponse.Recieverregion = appuseraddress.Region;
            invoiceResponse.Boxcount = (int)packageorderapplyexpress.Totalcount;
            invoiceResponse.Boxweight = (decimal)packageorderapplyexpress.Totalweight;
            invoiceResponse.Hasbattery = packageorderapplyexpressdetails.Any(t => t.Haselectrified == 1);

            invoiceResponse.Boxes = await packageQuery.ToListAsync();

            invoiceResponse.Ordercode = packageorderapply.Code;
            invoiceResponse.Appuserid = packageorderapply.Appuser;
            invoiceResponse.Appusercode = appuser.Usercode;
            invoiceResponse.Appusername = appuser.Name;
            invoiceResponse.Channelid = packageorderapply.Channelid;
            invoiceResponse.Channlename = channel.Name;
            invoiceResponse.Remoteprice = packageorderapplyexpressdetails.Sum(t => t.Remoteprice ?? 0);
            invoiceResponse.Remark = packageorderapply.Remark;
            invoiceResponse.CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
            //2 复制模板
            string demoFile = demofilePath + "InvoiceDemo.xlsx";
            string tempFile = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            XSSFWorkbook book ;
            //3 填写模板
            using (var fileStream = new FileStream(demoFile, FileMode.Open, FileAccess.Read))
            {
                book = new XSSFWorkbook(fileStream);
            }
            #region 写入Sheet
            ISheet sheet = book.GetSheetAt(0);

            //内单号
            ICell cell21 = sheet.GetRow(2).GetCell(1);
            cell21.SetCellValue(invoiceResponse.Ordercode);

            //件数
            ICell cell31 = sheet.GetRow(3).GetCell(1);
            cell31.SetCellValue(invoiceResponse.Boxcount);

            //重量
            ICell cell41 = sheet.GetRow(4).GetCell(1);
            cell41.SetCellValue((double)invoiceResponse.Boxweight);

            //收件人信息
            //公司名
            ICell cell80 = sheet.GetRow(8).GetCell(0);
            cell80.SetCellValue(invoiceResponse.Recievercompany);

            //地址
            ICell cell100 = sheet.GetRow(10).GetCell(0);
            cell100.SetCellValue(invoiceResponse.Recieveraddress);

            //城市
            ICell cell110 = sheet.GetRow(11).GetCell(0);
            cell110.SetCellValue(cell110.StringCellValue + invoiceResponse.Recieverregion);

            //省
            ICell cell120 = sheet.GetRow(12).GetCell(0);
            cell120.SetCellValue(cell120.StringCellValue + invoiceResponse.Recieverregion);

            //国家
            ICell cell130 = sheet.GetRow(13).GetCell(0);
            cell130.SetCellValue(cell130.StringCellValue + invoiceResponse.Recievercountryname);

            //手机号
            ICell cell140 = sheet.GetRow(14).GetCell(0);
            cell140.SetCellValue(cell140.StringCellValue + invoiceResponse.Recievermobile);

            //邮编
            ICell cell150 = sheet.GetRow(15).GetCell(0);
            cell150.SetCellValue(cell150.StringCellValue + invoiceResponse.Recievercode);

            //联系人
            ICell cell160 = sheet.GetRow(16).GetCell(0);
            cell160.SetCellValue(cell160.StringCellValue + invoiceResponse.Recievername);

            //电话
            ICell cell170 = sheet.GetRow(17).GetCell(0);
            cell170.SetCellValue(cell170.StringCellValue + invoiceResponse.Recievermobile);

            //寄件人信息
            //公司名
            ICell cell83 = sheet.GetRow(8).GetCell(3);
            cell83.SetCellValue(invoiceResponse.Sendcompany);

            //地址
            ICell cell103 = sheet.GetRow(10).GetCell(3);
            cell103.SetCellValue(invoiceResponse.Sendaddress);

            //城市
            ICell cell113 = sheet.GetRow(11).GetCell(3);
            cell113.SetCellValue(cell110.StringCellValue + invoiceResponse.Sendregion);

            //省
            ICell cell123 = sheet.GetRow(12).GetCell(3);
            cell123.SetCellValue(cell123.StringCellValue + invoiceResponse.Sendregion);

            //国家
            ICell cell133 = sheet.GetRow(13).GetCell(3);
            cell133.SetCellValue(cell133.StringCellValue + invoiceResponse.Sendcountry);

            //手机号
            ICell cell143 = sheet.GetRow(14).GetCell(3);
            cell143.SetCellValue(cell143.StringCellValue + invoiceResponse.Sendmobile);

            //邮编
            ICell cell153 = sheet.GetRow(15).GetCell(3);
            cell153.SetCellValue(cell153.StringCellValue + invoiceResponse.Sendcode);

            //联系人
            ICell cell163 = sheet.GetRow(16).GetCell(3);
            cell163.SetCellValue(cell163.StringCellValue + invoiceResponse.Sendname);

            //电话
            ICell cell173 = sheet.GetRow(17).GetCell(3);
            cell173.SetCellValue(cell173.StringCellValue + invoiceResponse.Sendmobile);

            //
            int startIndex = 20;
            int endIndex = 25;
            int currentIndex = 0;
            int calEndIndex = startIndex + invoiceResponse.Boxes.Count - 1;
            endIndex = calEndIndex <= endIndex ? calEndIndex : endIndex;
            for (int i = startIndex; i <= endIndex; i++)
            {
                ICell cell1i0 = sheet.GetRow(i).GetCell(0);
                cell1i0.SetCellValue(invoiceResponse.Boxes[currentIndex].Goodtype2name);

                ICell cell1i2 = sheet.GetRow(i).GetCell(2);
                cell1i2.SetCellValue(invoiceResponse.Boxes[currentIndex].PackageCount);

                ICell cell1i3 = sheet.GetRow(i).GetCell(3);
                cell1i3.SetCellValue(invoiceResponse.Boxes[currentIndex].Goodtype2code);
                currentIndex++;
            }

            //日期
            ICell cell1286 = sheet.GetRow(28).GetCell(6);
            cell1286.SetCellValue(DateTime.Now.ToString("yyyy-MM-dd"));

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

        public class InvoiceResponse
        {
            public long Id { get; set; }
            public string Sendcompany { get; set; }
            public string Sendcountry { get; set; }
            public string Sendcode { get; set; }
            public string Sendname { get; set; }
            public string Sendmobile { get; set; }
            public string Sendaddress { get; set; }
            public string Sendregion { get; set; }
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

            public List<InvoiceBoxdetail> Boxes { get; set; }
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
        public class InvoiceBoxdetail
        {
            public long Packageid { get; set; }
            public string Packagename { get; set; }
            public int PackageCount { get; set; }
            public long Goodtype1id { get; set; }
            public string Goodtype1name { get; set; }
            public long Goodtype2id { get; set; }
            public string Goodtype2name { get; set; }
            public string Goodtype2code { get; set; }
            public long Claimid { get; set; }
        }


    }
}
