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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 用户充值
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UseraccountchargeExportController : RickControllerBase
    {
        private readonly ILogger<UseraccountchargeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraccountService _appuseraccountService;
        private readonly RedisClientService _redisClientService;
        private string accountSubjectCode = "100";
        private readonly string filePath = "../Uploads/Temp/";
        public UseraccountchargeExportController(ILogger<UseraccountchargeController> logger, IAppuseraccountService appuseraccountService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraccountService = appuseraccountService;
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
        /// 导出用户充值
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="currencyid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string userCode, [FromQuery] string userName, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime,[FromQuery] int? status, [FromQuery] long? currencyid, [FromQuery] long? userid)
        {
            var query = from accountcharge in _appuseraccountService.Query<Appuseraccountcharge>(t => (!status.HasValue || t.Status == status) && (!currencyid.HasValue || t.Currencyid == currencyid) && (!userid.HasValue || t.Appuser == userid) && (!startTime.HasValue || t.Addtime >= startTime) && (!endTime.HasValue || t.Addtime<= endTime))
                        join user in _appuseraccountService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode) && (string.IsNullOrEmpty(userName) || t.Truename == userName) && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile))
                        on accountcharge.Appuser equals user.Id
                        join currency in _appuseraccountService.Query<Currency>(t => true)
                        on accountcharge.Currencyid equals currency.Id
                        into temp
                        from tc in temp.DefaultIfEmpty()
                        select new
                        {
                            Id = accountcharge.Id,
                            Userid = accountcharge.Appuser,
                            Username = user.Name,
                            Usercode = user.Usercode,
                            Usermobile = user.Mobile,
                            Currencyid = accountcharge.Currencyid,
                            CurrencyName = tc == null ? string.Empty : tc.Name,
                            Amount = accountcharge.Amount,
                            Status = accountcharge.Status,
                            Addtime = accountcharge.Addtime,
                            Paytype = accountcharge.Paytype
                        };
            int count = await query.CountAsync();

            var queryGroup = from q in query.OrderByDescending(t => t.Id)
                             join image in _appuseraccountService.Query<Appuseraccountchargeimage>(t => t.Status == 1)
                             on q.Id equals image.Appuseraccountchargeid
                             into imageTemp
                             from image in imageTemp.DefaultIfEmpty()
                             select new
                             {
                                 Id = q.Id,
                                 Userid = q.Userid,
                                 Username = q.Username,
                                 Usercode = q.Usercode,
                                 Usermobile = q.Usermobile,
                                 Paytype = q.Paytype,
                                 Currencyid = q.Currencyid,
                                 CurrencyName = q.CurrencyName,
                                 Amount = q.Amount,
                                 Status = q.Status,
                                 Addtime = q.Addtime,

                                 FileId = image == null ? 0 : image.Fileinfoid
                             };

            var queryR = from r in (await queryGroup.ToListAsync())
                         group r by new { r.Id, r.Userid, r.Username,r.Usercode,r.Usermobile ,r.Currencyid, r.CurrencyName, r.Amount, r.Status,r.Addtime,r.Paytype };

      
            var list = new List<UserAccountChargeResponse>();

            foreach (var r in queryR)
            {
                UserAccountChargeResponse userAccountChargeResponse = new UserAccountChargeResponse();
                userAccountChargeResponse.Images = new List<long>();
                userAccountChargeResponse.Id = r.Key.Id;
                userAccountChargeResponse.Userid = r.Key.Userid;
                userAccountChargeResponse.Username = r.Key.Username;
                userAccountChargeResponse.Usercode = r.Key.Usercode;
                userAccountChargeResponse.Usermobile = r.Key.Usermobile;
                userAccountChargeResponse.Currencyid = r.Key.Currencyid;
                userAccountChargeResponse.CurrencyName = r.Key.CurrencyName;
                userAccountChargeResponse.Amount = r.Key.Amount;
                userAccountChargeResponse.Addtime = r.Key.Addtime;
                userAccountChargeResponse.Status = r.Key.Status;
                userAccountChargeResponse.Paytype = r.Key.Paytype;
                foreach (var image in r)
                {
                    if (image.FileId != 0)
                    {
                        userAccountChargeResponse.Images.Add(image.FileId);
                    }
                }
                list.Add(userAccountChargeResponse);
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("收支核算");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("用户代码");
            head.CreateCell(1).SetCellValue("用户昵称");
            head.CreateCell(2).SetCellValue("用户手机号");
            head.CreateCell(3).SetCellValue("货币名称");
            head.CreateCell(4).SetCellValue("充值方式");
            head.CreateCell(5).SetCellValue("充值金额");
            head.CreateCell(6).SetCellValue("单据状态");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Usercode);
                row.CreateCell(1).SetCellValue(item.Username);
                row.CreateCell(2).SetCellValue(item.Usermobile);
                row.CreateCell(3).SetCellValue(item.CurrencyName);
                row.CreateCell(4).SetCellValue(Enum.GetName(typeof(PayType), item.Paytype));
                row.CreateCell(5).SetCellValue(item.Amount.ToString());
                row.CreateCell(6).SetCellValue(item.Status==0?"无效":item.Status==1?"已确认":"待确认");
   
                currentRow++;
            }


            string fileName = filePath + System.Guid.NewGuid().ToString("N") + ".xlsx";
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                book.Write(fileStream);
            }
            #endregion
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fileStream.Length));

                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }

        }

       

        public class UserAccountChargeResponse
        {
            public long Id { get; set; }

            public long Userid { get; set; }
            public string Username { get; set; }
            public string Usercode { get; set; }
            public string Usermobile { get; set; }

            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public decimal Amount { get; set; }
            public int Paytype { get; set; }

            public DateTime Addtime { get; set; }
            public int Status { get; set; }
            public List<long> Images { get; set; }
        }






    }
}
