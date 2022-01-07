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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Menu("仓库管理")]
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
        /// 查询仓库
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RepositoryResponceList>> Get([FromQuery] long? id, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            int count = await _repositoryService.CountAsync<Repository>(t => (!id.HasValue || t.Id == id) && (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name));

            var results = _repositoryService.Query<Repository>(t => (!id.HasValue || t.Id == id) && (!status.HasValue || t.Status == status) && (string.IsNullOrEmpty(name) || t.Name == name))
                .OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize);
            RepositoryResponceList repositoryResponceList = new RepositoryResponceList();
            repositoryResponceList.Count = count;
            repositoryResponceList.List = results.Select(repository => new RepositoryResponce()
            {
                Id = repository.Id,
                Name = repository.Name,
                Recivername = repository.Recivername,
                Recivermobil = repository.Recivermobil,
                Region = repository.Region,
                Address = repository.Address,
                Status = repository.Status
            }).ToList();
            var repositortIds = repositoryResponceList.List.Select(t => t.Id).ToList();
            var regions = await _repositoryService.QueryAsync<Repositoryregion>(t => t.Status == 1 && repositortIds.Contains(t.Repositoryid));
            var shelfs = await _repositoryService.QueryAsync<Repositoryshelf>(t => t.Status == 1 && repositortIds.Contains(t.Repositoryid));
            var layers = await _repositoryService.QueryAsync<Repositorylayer>(t => t.Status == 1 && repositortIds.Contains(t.Repositoryid));

            foreach (RepositoryResponce repositoryResponce in repositoryResponceList.List)
            {
                repositoryResponce.Regions = regions.Where(r => r.Repositoryid == repositoryResponce.Id).Select(r=>new RepositoryRegionResponse() { 
                    Id=r.Id,
                    Repositoryid= repositoryResponce.Id,
                    Name = r.Name,
                    Order = r.Order,
                    Status = r.Status,
                    Addtime = r.Addtime
                }).ToList();
                foreach (RepositoryRegionResponse repositoryRegionResponse in repositoryResponce.Regions) 
                {
                    repositoryRegionResponse.Shelfs = shelfs.Where(s => s.Repositoryid == repositoryResponce.Id && s.Repositoryregionid == repositoryRegionResponse.Id)
                        .Select(s => new RepositoryShelfResponse()
                        {
                            Id = s.Id,
                            Repositoryid = repositoryResponce.Id,
                            Repositoryregionid = repositoryRegionResponse.Id,
                            Name = s.Name,
                            Order = s.Order,
                            Status = s.Status,
                            Addtime = s.Addtime
                        }).ToList();
                    foreach (RepositoryShelfResponse repositoryShelfResponse in repositoryRegionResponse.Shelfs)
                    {
                        repositoryShelfResponse.Layers = layers.Where(s => s.Repositoryid == repositoryResponce.Id && s.Repositoryshelfid == repositoryShelfResponse.Id)
                        .Select(s => new RepositoryLayerResponse()
                        {
                            Id = s.Id,
                            Repositoryid = repositoryResponce.Id,
                            Repositoryshelfid = s.Id,
                            Name = s.Name,
                            Order = s.Order,
                            Status = s.Status,
                            Addtime = s.Addtime
                        }).ToList();
                    }
                }
            }

            return RickWebResult.Success(repositoryResponceList);
        }

        /// <summary>
        /// 新增仓库
        /// </summary>
        /// <param name="repositoryRequest"></param>
        /// <returns></returns>
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
            repository.Status = 1;
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

        /// <summary>
        /// 修改仓库
        /// </summary>
        /// <param name="repositoryPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<RepositoryResponce>> Put([FromBody] RepositoryPutRequest repositoryPutRequest)
        {
            await _repositoryService.BeginTransactionAsync();

            Repository repository = await _repositoryService.FindAsync<Repository>(t=>t.Id == repositoryPutRequest.Id);
            repository.Name = repositoryPutRequest.Name;
            repository.Recivername = repositoryPutRequest.Recivername;
            repository.Recivermobil = repositoryPutRequest.Recivermobil;
            repository.Region = repositoryPutRequest.Region;
            repository.Address = repositoryPutRequest.Address;
            repository.Status = repositoryPutRequest.Status;
            repository.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            repository.Lasttime = now;

            await _repositoryService.UpdateAsync(repository);
            await _repositoryService.CommitAsync();

            RepositoryResponce repositoryResponce = new RepositoryResponce();
            repositoryResponce.Id = repository.Id;
            repositoryResponce.Name = repository.Name;
            repositoryResponce.Recivername = repository.Recivername;
            repositoryResponce.Recivermobil = repository.Recivermobil;
            repositoryResponce.Region = repository.Region;
            repositoryResponce.Address = repository.Address;
            repositoryResponce.Status = repository.Status;

            return RickWebResult.Success(repositoryResponce);
        }

        /// <summary>
        /// 删除仓库
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _repositoryService.BeginTransactionAsync();

            var repositories = await _repositoryService.QueryAsync<Repository>(t => id == t.Id);
            foreach (var repository in repositories)
            {
                repository.Status = 0;
                repository.Lastuser = UserInfo.Id;
                DateTime now = DateTime.Now;
                repository.Lasttime = now;
                await _repositoryService.UpdateAsync(repository);
            }
            await _repositoryService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        public class RepositoryPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Recivername { get; set; }
            public string Recivermobil { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
            public int Status { get; set; }

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
            public int Status { get; set; }
            public List<RepositoryRegionResponse> Regions { get; set; }

        }
        public class RepositoryRegionResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public List<RepositoryShelfResponse> Shelfs { get; set; }

        }
        public class RepositoryShelfResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public long Repositoryregionid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
            public List<RepositoryLayerResponse> Layers { get; set; }

        }
        public class RepositoryLayerResponse
        {
            public long Id { get; set; }
            public long Repositoryid { get; set; }
            public long Repositoryshelfid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }

        }


        public class RepositoryResponceList
        {
            public int Count { get; set; }
            public List<RepositoryResponce> List { get; set; }
        }

    }
}
