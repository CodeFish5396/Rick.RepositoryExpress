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
        public async Task<RickWebResult<List<AgentResponse>>> Get([FromQuery] int? status)
        {
            var results = await _agentService.QueryAsync<Agent>(t => !status.HasValue || t.Status == status);
            var agentids = results.Select(t => t.Id);
            var courieres = await (from ac in _agentService.Query<Agentandcourier>(t => t.Status == 1 && agentids.Contains(t.Agentid))
                                   join c in _agentService.Query<Courier>(t => t.Status == 1)
                                   on ac.Courierid equals c.Id
                                   select new 
                                   {
                                       AgentId = ac.Agentid,
                                       CourierId = c.Id,
                                       CourierIdName = c.Name
                                   }
                                  ).ToListAsync();
            var retResults = results.Select(t => new AgentResponse()
            {
                Id = t.Id,
                Mobile = t.Mobile,
                Name = t.Name,
                Address = t.Address,
                Contact = t.Contact,
                Status = t.Status
            }).ToList();
            foreach (var ret in retResults)
            {
                ret.Courieres = courieres.Where(t => t.AgentId == ret.Id).Select(t => new AgentDetailResponse()
                {
                    CourierId = t.CourierId,
                    CourierIdName = t.CourierIdName
                }).Distinct().ToList();
            }

            return RickWebResult.Success(retResults);
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
        public async Task<RickWebResult<object>> Put([FromBody] AgentPutRequest agentPutRequest)
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

            var oldAgentCourieres = await _agentService.QueryAsync<Agentandcourier>(t => t.Agentid == agentPutRequest.Id && t.Status == 1);

            //删除旧的
            var deletedAC = oldAgentCourieres.Where(t => !agentPutRequest.Courieres.Any(c => c.CourierId == t.Courierid));
            foreach (var del in deletedAC)
            {
                del.Status = 0;
                await _agentService.UpdateAsync(del);
            }

            //添加新的
            var newAC = agentPutRequest.Courieres.Where(nc => !oldAgentCourieres.Any(oc => oc.Courierid == nc.CourierId));
            foreach (var newAgentC in newAC)
            {
                Agentandcourier agentandcourier = new Agentandcourier();
                agentandcourier.Id = _idGenerator.NextId();
                agentandcourier.Courierid = newAgentC.CourierId;
                agentandcourier.Agentid = agentPutRequest.Id;
                agentandcourier.Status = 1;
                await _agentService.AddAsync(agentandcourier);
            }

            await _agentService.CommitAsync();


            return RickWebResult.Success(new object());

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
        public List<AgentPutDetai> Courieres { get; set; }

    }

    public class AgentResponse
    {
        public long Id { get; set; }
        public string Contact { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }

        public string Name { get; set; }
        public int Status { get; set; }
        public List<AgentDetailResponse> Courieres { get; set; }

    }
    public class AgentDetailResponse
    {
        public long CourierId { get; set; }
        public string CourierIdName { get; set; }
    }

    public class AgentPutDetai
    {
        public long CourierId { get; set; }
    }

}
