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
    public class RoleController : RickControllerBase
    {
        private readonly ILogger<RoleController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRoleService _roleService;
        private readonly RedisClientService _redisClientService;

        public RoleController(ILogger<RoleController> logger, IRoleService roleService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _roleService = roleService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询角色
        /// </summary>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RolemenuListResponse>> Get([FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            RolemenuListResponse rolemenuListResponse = new RolemenuListResponse();
            var query = from role in _roleService.Query<Sysrole>(t => (string.IsNullOrEmpty(name) || t.Name == name)
                        && (!status.HasValue || t.Status == status)
                        && t.Companyid == UserInfo.Companyid
                        )
                        select new RolemenuResponse()
                        {
                            Id = role.Id,
                            Name = role.Name,
                            Companyid = role.Companyid,
                            Status = role.Status,
                            Order = role.Order,
                            Addtime = role.Addtime,
                            Isdefault = role.Isdefault
                        };

            rolemenuListResponse.Count = await query.CountAsync();
            rolemenuListResponse.List = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            return RickWebResult.Success(rolemenuListResponse);
        }

        /// <summary>
        /// 新增角色
        /// </summary>
        /// <param name="rolePostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] RolePostRequest rolePostRequest)
        {
            await _roleService.BeginTransactionAsync();

            Sysrole sysrole = new Sysrole();
            DateTime now = DateTime.Now;
            sysrole.Id = _idGenerator.NextId();
            sysrole.Name = rolePostRequest.Name;
            sysrole.Companyid = UserInfo.Companyid;
            sysrole.Status = 1;
            sysrole.Isdefault = 0;
            sysrole.Addtime = now;
            sysrole.Lasttime = now;
            sysrole.Adduser = UserInfo.Id;
            sysrole.Lastuser = UserInfo.Id;

            await _roleService.AddAsync(sysrole);
            await _roleService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Detete([FromQuery] long id)
        {
            await _roleService.BeginTransactionAsync();

            Sysrole sysrole = await _roleService.FindAsync<Sysrole>(id);
            DateTime now = DateTime.Now;
            sysrole.Status = 0;
            sysrole.Lasttime = now;
            sysrole.Lastuser = UserInfo.Id;

            await _roleService.UpdateAsync(sysrole);
            await _roleService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改角色
        /// </summary>
        /// <param name="rolePostPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] RolePostPutRequest rolePostPutRequest)
        {
            await _roleService.BeginTransactionAsync();

            Sysrole sysrole = await _roleService.FindAsync<Sysrole>(rolePostPutRequest.Id);
            sysrole.Name = rolePostPutRequest.Name;
            DateTime now = DateTime.Now;
            sysrole.Status = 0;
            sysrole.Lasttime = now;
            sysrole.Lastuser = UserInfo.Id;


            await _roleService.UpdateAsync(sysrole);
            await _roleService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class RolemenuListResponse
        {
            public int Count { get; set; }
            public List<RolemenuResponse> List { get; set; }
        }
        public class RolemenuResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public long Companyid { get; set; }
            public int Status { get; set; }
            public int Order { get; set; }
            public sbyte Isdefault { get; set; }
            public DateTime Addtime { get; set; }
        }
        public class RolePostRequest
        {
            public string Name { get; set; }
        }
        public class RolePostPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }


        //public class RolemenuTreeResponse
        //{
        //    public long MenuId { get; set; }
        //    public string Index { get; set; }
        //    public string Name { get; set; }
        //    public long? Parentid { get; set; }
        //    public sbyte Isdirectory { get; set; }
        //    public int Order { get; set; }
        //    public List<RolemenuTreeResponse> Subs { get; set; }
        //    public List<RolemenuFunctionResponse> Functions { get; set; }
        //}
        //public class RolemenuFunctionResponse
        //{
        //    public long Id { get; set; }
        //    public long RoleId { get; set; }
        //    public long MenuId { get; set; }
        //    public long FunctionId { get; set; }
        //    public string FunctionName { get; set; }
        //    public string FunctionTypeName { get; set; }
        //}
    }
}
