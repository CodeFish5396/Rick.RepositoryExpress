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
    /// 订单财务清账
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApplyAccountExportController : RickControllerBase
    {
        private readonly ILogger<OrderApplyAccountController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;
        private readonly string filePath = "../Uploads/Temp/";
        public OrderApplyAccountExportController(ILogger<OrderApplyAccountController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
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
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Temp\\";
            }
            else
            {
                filePath = dr + "/Uploads/Temp/";
            }
        }

        /// <summary>
        /// 导出用户确认发货
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userCode"></param>
        /// <param name="agentId"></param>
        /// <param name="userMobile"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="orderStatus"></param>
        /// <param name="sendUserName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileContentResult> Get([FromQuery] string code, [FromQuery] string userCode, [FromQuery] long? agentId, [FromQuery] string userMobile, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime, [FromQuery] int? status, [FromQuery] int? orderStatus, [FromQuery] string sendUserName)
        {
            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => (!status.HasValue || t.Status == status)
                        && (!orderStatus.HasValue || t.Orderstatus == orderStatus)
                        && (t.Orderstatus == (int)OrderApplyStatus.待发货 || t.Orderstatus == (int)OrderApplyStatus.已发货 || t.Orderstatus == (int)OrderApplyStatus.已签收)
                        && (string.IsNullOrEmpty(code) || t.Code == code)
                        && (!startTime.HasValue || t.Sendtime >= startTime)
                        && (!endTime.HasValue || t.Sendtime <= endTime)
                        )
                        join packageorderapplyexpress in _packageOrderApplyService.Query<Packageorderapplyexpress>(t => t.Status == 1 && (!agentId.HasValue || t.Agentid == agentId))
                        on order.Id equals packageorderapplyexpress.Packageorderapplyid
                        join channel in _packageOrderApplyService.Query<Channel>(t => true)
                        on order.Channelid equals channel.Id
                        join nation in _packageOrderApplyService.Query<Nation>(t => true)
                        on order.Nationid equals nation.Id
                        join appuser in _packageOrderApplyService.Query<Appuser>(t => (string.IsNullOrEmpty(userCode) || t.Usercode == userCode)
                            && (string.IsNullOrEmpty(userMobile) || t.Mobile == userMobile)
                        )
                        on order.Appuser equals appuser.Id
                        join address in _packageOrderApplyService.Query<Appuseraddress>()
                        on order.Addressid equals address.Id
                        join sysUser in _packageOrderApplyService.Query<Sysuser>()
                        on order.Senduser equals sysUser.Id
                        into sysUserTemp
                        from sysUser in sysUserTemp.DefaultIfEmpty()
                        join courier in _packageOrderApplyService.Query<Courier>()
                        on packageorderapplyexpress.Courierid equals courier.Id
                        into courierTmp
                        from courier in courierTmp.DefaultIfEmpty()
                        join agent in _packageOrderApplyService.Query<Agent>()
                        on packageorderapplyexpress.Agentid equals agent.Id
                        into agentTmp
                        from agent in agentTmp.DefaultIfEmpty()
                        where (string.IsNullOrEmpty(sendUserName) || (sysUser != null && sysUser.Name == sendUserName))
                        select new OrderApplyAccountResponse()
                        {
                            Id = order.Id,
                            Expressid = packageorderapplyexpress.Id,
                            Code = order.Code,
                            Channelid = order.Channelid,
                            Channelname = channel.Name,
                            Nationid = order.Nationid,
                            Nationname = nation.Name,
                            Addressid = order.Addressid,
                            AddressName = address.Name,
                            AddressContactnumber = address.Contactnumber,
                            AddressRegion = address.Region,
                            AddressAddress = address.Address,
                            Appuser = order.Appuser,
                            Appusercode = appuser.Usercode,
                            Appusername = appuser.Truename,
                            Appusermobile = appuser.Mobile,
                            Orderstatus = order.Orderstatus,
                            Ispayed = order.Ispayed ?? 0,
                            Paytime = order.Paytime,
                            Price = packageorderapplyexpress.Price ?? 0,
                            Targetprice = packageorderapplyexpress.Targetprice ?? 0,
                            Freightprice = packageorderapplyexpress.Freightprice ?? 0,
                            Outnumber = packageorderapplyexpress.Outnumber,
                            Innernumber = packageorderapplyexpress.Innernumber,
                            Agentid = packageorderapplyexpress.Agentid,
                            Agentname = agent == null ? string.Empty : agent.Name,
                            Agentprice = packageorderapplyexpress.Agentprice ?? 0,
                            Localagentprice = packageorderapplyexpress.Localagentprice ?? 0,
                            Courierid = packageorderapplyexpress.Courierid,
                            Couriername = courier == null ? string.Empty : courier.Name,
                            Status = order.Status,
                            Addtime = order.Addtime,
                            Senduser = sysUser == null ? 0 : sysUser.Id,
                            Sendusername = sysUser == null ? string.Empty : sysUser.Name,
                            Sendtime = order.Sendtime,
                            Isagentpayed = order.Isagentpayed,
                            Agentpaytime = order.Agentpaytime,

                        };


            var list = await query.OrderByDescending(t => t.Addtime).ToListAsync();

            var userPayOrderids = list.Where(t => t.Ispayed == 1).Select(t => t.Id).ToList();
            var agentPayOrderids = list.Where(t => t.Isagentpayed == 1).Select(t => t.Id).ToList();

            var appuseraccountconsumes = await _packageOrderApplyService.QueryAsync<Appuseraccountconsume>(t => userPayOrderids.Contains((long)t.Orderid) && t.Status == 1);
            var agentfeeconsumes = await _packageOrderApplyService.QueryAsync<Agentfeeconsume>(t => agentPayOrderids.Contains((long)t.Orderid) && t.Status == 1);

            List<long> currenciesIds = new List<long>();
            List<long> userIds = new List<long>();
            foreach (var item in list)
            {
                if (item.Ispayed == 1)
                {
                    item.UserPay = appuseraccountconsumes.Where(t => t.Orderid == item.Id).Select(t => new UserPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Curencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.UserPay != null)
                    {
                        if (!currenciesIds.Contains(item.UserPay.Currencyid))
                        {
                            currenciesIds.Add(item.UserPay.Currencyid);
                        }
                        if (!userIds.Contains(item.UserPay.Userid))
                        {
                            userIds.Add(item.UserPay.Userid);
                        }
                    }
                }
                if (item.Isagentpayed == 1)
                {
                    item.AgentPay = agentfeeconsumes.Where(t => t.Orderid == item.Id).Select(t => new AgentPay()
                    {
                        Paytime = t.Addtime,
                        Userid = t.Adduser,
                        Currencyid = (long)t.Currencyid,
                        Amount = t.Amount
                    }).SingleOrDefault();
                    if (item.AgentPay != null)
                    {
                        if (!currenciesIds.Contains(item.AgentPay.Currencyid))
                        {
                            currenciesIds.Add(item.AgentPay.Currencyid);
                        }
                        if (!userIds.Contains(item.AgentPay.Userid))
                        {
                            userIds.Add(item.AgentPay.Userid);
                        }
                    }
                }
            }

            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => currenciesIds.Contains(t.Id));
            var useres = await _packageOrderApplyService.QueryAsync<Sysuser>(t => userIds.Contains(t.Id));

            foreach (var item in list)
            {
                if (item.UserPay != null)
                {
                    item.UserPay.Currencyname = currencies.Single(t => t.Id == item.UserPay.Currencyid).Name;
                    item.UserPay.Username = useres.Single(t => t.Id == item.UserPay.Userid).Name;
                }
                if (item.AgentPay != null)
                {
                    item.AgentPay.Currencyname = currencies.Single(t => t.Id == item.AgentPay.Currencyid).Name;
                    item.AgentPay.Username = useres.Single(t => t.Id == item.AgentPay.Userid).Name;
                }
            }

            #region 写入Sheet
            XSSFWorkbook book = new XSSFWorkbook();
            ISheet sheet = book.CreateSheet("订单清账");

            //标题
            int currentRow = 0;
            IRow head = sheet.CreateRow(currentRow);
            head.CreateCell(0).SetCellValue("客户代码");
            head.CreateCell(1).SetCellValue("发货渠道");
            head.CreateCell(2).SetCellValue("收货国家");
            head.CreateCell(3).SetCellValue("内单号");
            head.CreateCell(4).SetCellValue("支付金额");
            head.CreateCell(5).SetCellValue("用户支付时间");
            head.CreateCell(6).SetCellValue("用户支付状态");
            head.CreateCell(7).SetCellValue("代理支付时间");
            head.CreateCell(8).SetCellValue("代理支付状态");
            head.CreateCell(9).SetCellValue("订单状态");
            head.CreateCell(10).SetCellValue("用户支付经手人");
            head.CreateCell(11).SetCellValue("代理支付经手人");
            currentRow++;

            foreach (var item in list)
            {
                IRow row = sheet.CreateRow(currentRow);
                row.CreateCell(0).SetCellValue(item.Appusercode);
                row.CreateCell(1).SetCellValue(item.Channelname);
                row.CreateCell(2).SetCellValue(item.Nationname);
                row.CreateCell(3).SetCellValue(item.Code);
                row.CreateCell(4).SetCellValue(item.Price.HasValue?item.Price.Value.ToString():"无");
                row.CreateCell(5).SetCellValue(item.Paytime.ToString());
                row.CreateCell(6).SetCellValue(item.Ispayed==1 ? "已支付" : "未支付");
                row.CreateCell(7).SetCellValue(item.Agentpaytime.ToString());
                row.CreateCell(8).SetCellValue(item.Isagentpayed == 1 ? "已支付" : "未支付");
                row.CreateCell(9).SetCellValue(Enum.GetName(typeof(OrderApplyStatus), item.Orderstatus));
                row.CreateCell(10).SetCellValue(item.Ispayed == 1 ? item.UserPay.Username : "未支付");
                row.CreateCell(11).SetCellValue(item.Isagentpayed == 1 ? item.AgentPay.Username : "未支付");

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

        
        public class OrderApplyAccountResquest
        {
            public List<long> Ids { get; set; }
            public long Currencyid { get; set; }
        }
        public class OrderApplyAccountResponse
        {
            public string Code { get; set; }
            public long Id { get; set; }
            public long Expressid { get; set; }
            public long Channelid { get; set; }
            public string Channelname { get; set; }
            public long Nationid { get; set; }
            public string Nationname { get; set; }
            public long Addressid { get; set; }
            public string AddressName { get; set; }
            public string AddressContactnumber { get; set; }
            public string AddressRegion { get; set; }
            public string AddressAddress { get; set; }

            public long Appuser { get; set; }
            public string Appusername { get; set; }
            public string Appusercode { get; set; }
            public string Appusermobile { get; set; }
            public int Orderstatus { get; set; }
            public sbyte? Ispayed { get; set; }
            public DateTime? Paytime { get; set; }
            public sbyte Isagentpayed { get; set; }
            public DateTime? Agentpaytime { get; set; }

            public decimal? Price { get; set; }
            public decimal? Targetprice { get; set; }

            public decimal? Freightprice { get; set; }

            public string Outnumber { get; set; }
            public string Innernumber { get; set; }
            public long? Agentid { get; set; }
            public string Agentname { get; set; }
            public long? Courierid { get; set; }
            public string Couriername { get; set; }
            public decimal? Localagentprice { get; set; }

            public decimal? Agentprice { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public long Senduser { get; set; }
            public string Sendusername { get; set; }
            public DateTime? Sendtime { get; set; }
            public UserPay UserPay { get; set; }
            public AgentPay AgentPay { get; set; }
        }

        public class OrderApplyAccountPutResquest
        {
            public List<long> Ids { get; set; }
        }


    }
   

}
