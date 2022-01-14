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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentDataController : RickControllerBase
    {
        private readonly ILogger<AgentDataController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentService _agentService;
        private readonly RedisClientService _redisClientService;

        public AgentDataController(ILogger<AgentDataController> logger, IAgentService agentService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentService = agentService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }
        



    }
}
