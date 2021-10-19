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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    /// <summary>
    /// 快递公司
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CourierController : RickControllerBase
    {
        private readonly ILogger<CourierController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ICourierService _courierService;
        private readonly RedisClientService _redisClientService;

        public CourierController(ILogger<CourierController> logger, ICourierService courierService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _courierService = courierService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询快递公司
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<CourierResponseList>> Get([FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            int count = await _courierService.CountAsync<Courier>(t => (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name));
            var results = _courierService.Query<Courier>(t => (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name))
                .OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize);
            CourierResponseList courierResponseList = new CourierResponseList();
            courierResponseList.Count = count;
            courierResponseList.List = results.Select(t => new CourierResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Extname = t.Extname,
                Status = t.Status,
                Addtime = t.Addtime
            }); 
            return RickWebResult.Success(courierResponseList);
        }

        /// <summary>
        /// 创建快递公司
        /// </summary>
        /// <param name="courierRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<CourierResponse>> Post([FromBody] CourierRequest courierRequest)
        {
            await _courierService.BeginTransactionAsync();

            Courier courier = new Courier();
            DateTime now = DateTime.Now;
            courier.Id = _idGenerator.NextId();
            courier.Name = courierRequest.Name;
            courier.Code = courierRequest.Code;
            courier.Extname = courierRequest.Extname;
            courier.Status = 1;
            courier.Addtime = now;
            courier.Lasttime = now;
            courier.Adduser = UserInfo.Id;
            courier.Lastuser = UserInfo.Id;
            await _courierService.AddAsync(courier);
            await _courierService.CommitAsync();
            CourierResponse courierResponse = new CourierResponse();
            courierResponse.Id = courier.Id;
            courierResponse.Name = courier.Name;
            courierResponse.Code = courier.Code;
            courierResponse.Extname = courier.Extname;
            courierResponse.Status = courier.Status;
            return RickWebResult.Success(courierResponse);
        }

    }

    public class CourierRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Extname { get; set; }
    }

    public class CourierResponse
    {
        public long Id { get; set; }
        public string Code { get; set; }

        public string Name { get; set; }
        public string Extname { get; set; }
        public int Status { get; set; }
        public DateTime Addtime { get; set; }
    }
    public class CourierResponseList
    {
        public int Count { get; set; }
        public IEnumerable<CourierResponse> List { get; set; }
    }

}
