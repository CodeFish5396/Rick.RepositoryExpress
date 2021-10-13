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

        [HttpGet]
        public async Task<RickWebResult<IEnumerable<RepositoryResponce>>> Get([FromQuery] long id)
        {
            var result = (await _repositoryService.QueryAsync<Repository>(t => t.Companyid == UserInfo.Companyid && (t.Id == id || id <= 0)))
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

        [HttpPost]
        public async Task<RickWebResult<RepositoryResponce>> Post([FromBody] RepositoryRequest repositoryRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repository repository = new Repository();
            repository.Id = _idGenerator.NextId();
            repository.Companyid = UserInfo.Companyid;
            repository.Name = repositoryRequest.Name;
            repository.Recivername = repositoryRequest.Recivername;
            repository.Recivermobil = repositoryRequest.Recivermobil;
            repository.Region = repositoryRequest.Region;
            repository.Address = repositoryRequest.Address;
            repository.Satus = 1;
            repository.Adduser = UserInfo.Id;
            repository.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repository.Addtime = now;
            repository.Lasttime = now;
            await _repositoryService.AddAsync(repository);
            await _repositoryService.CommitAsync();
            RepositoryResponce repositoryResponce = new RepositoryResponce();
            repositoryResponce.Id = repository.Id;
            repositoryResponce.Name = repository.Name;
            repositoryResponce.Recivername = repository.Recivername;
            repositoryResponce.Recivermobil = repository.Recivermobil;
            repositoryResponce.Region = repository.Region;
            repositoryResponce.Address = repository.Address;

            return RickWebResult.Success(repositoryResponce);
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
