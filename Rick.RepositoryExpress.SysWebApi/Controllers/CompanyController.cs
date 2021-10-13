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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : RickControllerBase
    {
        private readonly ILogger<CompanyController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISyscompanyService _syscompanyService;
        private readonly RedisClientService _redisClientService;

        public CompanyController(ILogger<CompanyController> logger, ISyscompanyService syscompanyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _syscompanyService = syscompanyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

    }
}
