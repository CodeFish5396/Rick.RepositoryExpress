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
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderExpressController : RickControllerBase
    {
        private readonly ILogger<OrderExpressController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageorderapplyexpressService _packageorderapplyexpressService;
        private readonly RedisClientService _redisClientService;

        public OrderExpressController(ILogger<OrderExpressController> logger, IPackageorderapplyexpressService packageorderapplyexpressService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageorderapplyexpressService = packageorderapplyexpressService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }


        
    }
}
