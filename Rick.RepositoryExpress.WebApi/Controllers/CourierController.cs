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
using Rick.RepositoryExpress.Utils;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
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
        /// 查询所有快递
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<CourierResponse>>> Get()
        {
            var result = await _courierService.QueryAsync<Courier>(nation => nation.Status == 1);
            return RickWebResult.Success(result.OrderBy(courier => courier.Id).Select(courier => new CourierResponse()
            {
                Id = courier.Id,
                Code = courier.Code,
                Name = courier.Name
            }));

        }

        public class CourierRequest
        { 
        
        }
        public class CourierResponse
        {
            public long Id { get; set; }

            public string Code { get; set; }
            public string Name { get; set; }

        }
        public class CourierResponseList
        {

        }
    }
}
