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
        private readonly ILogger<DefaultController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ICourierService _courierService ;
        private readonly RedisClientService _redisClientService;

        public CourierController(ILogger<DefaultController> logger, ICourierService courierService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _courierService = courierService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        [HttpGet]
        public async Task<RickWebResult<IEnumerable<CourierResponse>>> Get()
        {
            
        }

        [HttpPost]
        public async Task<RickWebResult<UserLoginResult>> Post([FromBody] CourierRequest courierRequest)
        {

        }

    }

    public class CourierRequest
    {
        public string Name { get; set; }
        public string Extname { get; set; }
    }

    public class CourierResponse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Extname { get; set; }
        public int Status { get; set; }
    }

}
