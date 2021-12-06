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
    public class RepositoryController : RickControllerBase
    {
        private readonly ILogger<RepositoryController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRepositoryService _repositoryService;
        private readonly RedisClientService _redisClientService;

        public RepositoryController(ILogger<RepositoryController> logger, IRepositoryService repositoryService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取仓库列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<RepositoryResponce>>> Get([FromQuery] long id)
        {
            var result = (await _repositoryService.QueryAsync<Repository>(t => t.Status == 1 && t.Companyid == UserInfo.Companyid && (t.Id == id || id <= 0)))
                .Select(repository => new RepositoryResponce()
                {
                    Id = repository.Id,
                    Name = repository.Name,
                    Recivername = repository.Recivername,
                    Recivermobil = repository.Recivermobil,
                    Region = repository.Region,
                    Address = repository.Address,
                });
            return RickWebResult.Success(result);
        }

        public class RepositoryRequest
        {
            public string Name { get; set; }
            public string Recivername { get; set; }
            public string Recivermobil { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
        }
        public class RepositoryResponce
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Recivername { get; set; }
            public string Recivermobil { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
        }
    }
}
