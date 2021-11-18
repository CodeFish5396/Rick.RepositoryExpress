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
    /// 代理商
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : RickControllerBase
    {
        private readonly ILogger<AgentController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAgentService _agentService;
        private readonly RedisClientService _redisClientService;

        public AgentController(ILogger<AgentController> logger, IAgentService agentService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _agentService = agentService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询代理商
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<AgentResponse>>> Get([FromQuery] int? status)
        {
            var results = await _agentService.QueryAsync<Agent>(t => !status.HasValue || t.Status == status);
            return RickWebResult.Success(results.Select(t => new AgentResponse()
            {
                Id = t.Id,
                Mobile = t.Mobile,
                Name = t.Name,
                Address = t.Address,
                Contact = t.Contact,
                Status = t.Status
            }));
        }

        /// <summary>
        /// 创建代理商
        /// </summary>
        /// <param name="agentRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<AgentResponse>> Post([FromBody] AgentRequest agentRequest)
        {
            await _agentService.BeginTransactionAsync();

            Agent agent = new Agent();
            DateTime now = DateTime.Now;
            agent.Id = _idGenerator.NextId();
            agent.Name = agentRequest.Name;
            agent.Contact = agentRequest.Contact;
            agent.Mobile = agentRequest.Mobile;
            agent.Address = agentRequest.Address;

            agent.Status = 1;
            agent.Addtime = now;
            agent.Lasttime = now;
            agent.Adduser = UserInfo.Id;
            agent.Lastuser = UserInfo.Id;
            await _agentService.AddAsync(agent);
            await _agentService.CommitAsync();
            AgentResponse agentResponse = new AgentResponse();
            agentResponse.Id = agent.Id;
            agentResponse.Name = agent.Name;
            agentResponse.Contact = agent.Contact;
            agentResponse.Mobile = agent.Mobile;
            agentResponse.Address = agent.Address;

            agentResponse.Status = agent.Status;
            return RickWebResult.Success(agentResponse);

        }

        /// <summary>
        /// 修改代理商
        /// </summary>
        /// <param name="agentPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<AgentResponse>> Put([FromBody] AgentPutRequest agentPutRequest)
        {
            await _agentService.BeginTransactionAsync();

            Agent agent = await _agentService.FindAsync<Agent>(t => t.Id == agentPutRequest.Id);
            DateTime now = DateTime.Now;
            agent.Name = agentPutRequest.Name;
            agent.Contact = agentPutRequest.Contact;
            agent.Mobile = agentPutRequest.Mobile;
            agent.Address = agentPutRequest.Address;

            agent.Status = 1;
            agent.Lasttime = now;
            agent.Lastuser = UserInfo.Id;
            await _agentService.UpdateAsync(agent);
            await _agentService.CommitAsync();
            AgentResponse agentResponse = new AgentResponse();
            agentResponse.Id = agent.Id;
            agentResponse.Name = agent.Name;
            agentResponse.Contact = agent.Contact;
            agentResponse.Mobile = agent.Mobile;
            agentResponse.Address = agent.Address;

            agentResponse.Status = agent.Status;
            return RickWebResult.Success(agentResponse);

        }

        /// <summary>
        /// 删除代理商
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _agentService.BeginTransactionAsync();

            Agent agent = await _agentService.FindAsync<Agent>(t => t.Id == id);
            DateTime now = DateTime.Now;

            agent.Status = 0;
            agent.Lasttime = now;
            agent.Lastuser = UserInfo.Id;
            await _agentService.UpdateAsync(agent);
            await _agentService.CommitAsync();
            AgentResponse agentResponse = new AgentResponse();
            agentResponse.Id = agent.Id;
            agentResponse.Name = agent.Name;
            agentResponse.Contact = agent.Contact;
            agentResponse.Mobile = agent.Mobile;
            agentResponse.Address = agent.Address;

            agentResponse.Status = agent.Status;
            return RickWebResult.Success(new object());

        }

    }

    public class AgentRequest
    {
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
    }

    public class AgentPutRequest
    {
        public long Id { get; set; }
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
    }

    public class AgentResponse
    {
        public long Id { get; set; }
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }

        public string Name { get; set; }
        public int Status { get; set; }
    }

}
