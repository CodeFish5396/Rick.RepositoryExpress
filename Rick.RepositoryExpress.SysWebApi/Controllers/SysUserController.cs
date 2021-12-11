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
    public class SysUserController : RickControllerBase
    {
        private readonly ILogger<SysUserController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public SysUserController(ILogger<SysUserController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 系统用户查询
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<SysUserResponseList>> Get([FromQuery] string mobile, [FromQuery] string name, [FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            SysUserResponseList sysUserResponseList = new SysUserResponseList();
            var query = from user in _sysuserService.Query<Sysuser>(t => t.Name != "root" && (!status.HasValue || t.Status == status)
                        && (string.IsNullOrEmpty(mobile) || t.Mobile == mobile)
                        && (string.IsNullOrEmpty(name) || t.Name == name)
                        )
                        select new SysUserResponse()
                        {
                            Id = user.Id,
                            Mobile = user.Mobile,
                            Name = user.Name,
                            Addtime = user.Addtime,
                            Status = user.Status
                        };
            sysUserResponseList.Count = await query.CountAsync();
            sysUserResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            var userIds = sysUserResponseList.List.Select(t => t.Id);
            var userRoles = await _sysuserService.QueryAsync<Sysuserrole>(t => t.Status == 1 && t.Companyid == UserInfo.Companyid && userIds.Contains(t.Userid));
            var roleIds = userRoles.Select(t => t.Roleid);
            var roles = await _sysuserService.QueryAsync<Sysrole>(t => t.Companyid == UserInfo.Companyid && t.Status == 1);

            foreach (var user in sysUserResponseList.List)
            {
                user.Roles =  (from role in roles
                              select new SysUserRoleResponse()
                              {
                                  Roleid = role.Id,
                                  Rolename = role.Name,
                              }).ToList();
                foreach (var userrole in user.Roles)
                {
                    userrole.IsChecked = userRoles.Any(t => t.Userid == user.Id && t.Roleid == userrole.Roleid);
                }
            }

            return RickWebResult.Success(sysUserResponseList);
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        /// <param name="sysUserRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] SysUserRequest sysUserRequest)
        {
            await _sysuserService.BeginTransactionAsync();

            Sysuser sysuser = new Sysuser();
            DateTime now = DateTime.Now;
            sysuser.Id = _idGenerator.NextId();
            sysuser.Name = sysUserRequest.Name;
            sysuser.Mobile = sysUserRequest.Mobile;
            sysuser.Password = Md5Helper.Create("a1Ab2Bc3C");
            sysuser.Status = 1;
            sysuser.Addtime = now;
            sysuser.Lasttime = now;
            sysuser.Adduser = UserInfo.Id;
            sysuser.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(sysuser);

            Sysusercompany sysusercompany = new Sysusercompany();
            sysusercompany.Id = _idGenerator.NextId();
            sysusercompany.Userid = sysuser.Id;
            sysusercompany.Companyid = UserInfo.Companyid;
            sysusercompany.Status = 1;
            sysusercompany.Addtime = now;
            sysusercompany.Lasttime = now;
            sysusercompany.Adduser = UserInfo.Id;
            sysusercompany.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(sysusercompany);

            await _sysuserService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改用户，添加权限
        /// </summary>
        /// <param name="sysUserPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] SysUserPutRequest sysUserPutRequest)
        {
            await _sysuserService.BeginTransactionAsync();

            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Id == sysUserPutRequest.Id);
            DateTime now = DateTime.Now;
            sysuser.Name = sysUserPutRequest.Name;
            sysuser.Mobile = sysUserPutRequest.Mobile;
            sysuser.Password = Md5Helper.Create("a1Ab2Bc3C");
            sysuser.Status = 1;
            sysuser.Lasttime = now;
            sysuser.Lastuser = UserInfo.Id;
            await _sysuserService.UpdateAsync(sysuser);

            var olduserRoles = await _sysuserService.QueryAsync<Sysuserrole>(t => t.Status == 1 && t.Companyid == UserInfo.Companyid && t.Userid == sysUserPutRequest.Id);

            var currentCheckedRoles = sysUserPutRequest.Roles.Where(t => t.IsChecked);

            //1、新增角色权限
            var newRoles = currentCheckedRoles.Where(currentCheckedRole => !olduserRoles.Any(t => currentCheckedRole.Roleid == t.Roleid));
            foreach (SysUserRolePutRequest sysUserRolePutRequest in newRoles)
            {
                Sysuserrole sysuserrole = new Sysuserrole();
                sysuserrole.Id = _idGenerator.NextId();
                sysuserrole.Userid = sysUserPutRequest.Id;
                sysuserrole.Roleid = sysUserRolePutRequest.Roleid;
                sysuserrole.Companyid = UserInfo.Companyid;
                sysuserrole.Status = 1;
                sysuserrole.Addtime = now;
                sysuserrole.Lasttime = now;
                sysuserrole.Adduser = UserInfo.Id;
                sysuserrole.Lastuser = UserInfo.Id;
                await _sysuserService.AddAsync(sysuserrole);
            }

            //2、删除角色权限
            var deleteRoles = olduserRoles.Where(olduserRole => !currentCheckedRoles.Any(t => t.Roleid == olduserRole.Roleid));
            foreach (Sysuserrole deleteRole in deleteRoles)
            {
                deleteRole.Status = 0;
                deleteRole.Lasttime = now;
                deleteRole.Lastuser = UserInfo.Id;
                await _sysuserService.UpdateAsync(deleteRole);
            }

            await _sysuserService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPatch]
        public async Task<RickWebResult<object>> Patch([FromQuery] long id)
        {
            await _sysuserService.BeginTransactionAsync();

            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Id == id);
            DateTime now = DateTime.Now;
            sysuser.Password = Md5Helper.Create("a1Ab2Bc3C");
            sysuser.Lasttime = now;
            sysuser.Lastuser = UserInfo.Id;
            await _sysuserService.UpdateAsync(sysuser);
            await _sysuserService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class SysUserResponse
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Name { get; set; }
            public DateTime Addtime { get; set; }
            public int Status { get; set; }
            public List<SysUserRoleResponse> Roles { get; set; }
        }
        public class SysUserResponseList
        {
            public int Count { get; set; }
            public List<SysUserResponse> List { get; set; }
        }
        public class SysUserRequest
        {
            public string Mobile { get; set; }
            public string Name { get; set; }
        }
        public class SysUserPutRequest
        {
            public long Id { get; set; }
            public string Mobile { get; set; }
            public string Name { get; set; }
            public List<SysUserRolePutRequest> Roles { get; set; }

        }
        public class SysUserRolePutRequest
        {
            public long Roleid { get; set; }
            public string Rolename { get; set; }
            public bool IsChecked { get; set; }

        }

        public class SysUserRoleResponse
        {
            public long Roleid { get; set; }
            public string Rolename { get; set; }
            public bool IsChecked { get; set; }
        }

    }
}
