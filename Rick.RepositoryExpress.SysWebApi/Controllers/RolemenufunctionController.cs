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
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolemenufunctionController : RickControllerBase
    {
        private readonly ILogger<RolemenufunctionController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IRoleService _roleService;
        private readonly RedisClientService _redisClientService;

        public RolemenufunctionController(ILogger<RolemenufunctionController> logger, IRoleService roleService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _roleService = roleService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询角色权限
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<RolemenufunctionResponse>> Get([FromQuery] long id)
        {
            RolemenufunctionResponse rolemenufunctionResponse = new RolemenufunctionResponse();
            var role = await _roleService.FindAsync<Sysrole>(id);
            rolemenufunctionResponse.Id = role.Id;
            rolemenufunctionResponse.Name = role.Name;
            string sysmenuTreeResponsesString =  _redisClientService.StringGet(ConstString.RickMenuTreeKey);

            rolemenufunctionResponse.Details = JsonConvert.DeserializeObject<List<SysmenuTreeResponse>>(sysmenuTreeResponsesString);

            var Sysrolemenufunctions = await _roleService.QueryAsync<Sysrolemenufunction>(t => t.Roleid == id && t.Companyid == UserInfo.Companyid && t.Status == 1);
            foreach (var fisrtLayerMenu in rolemenufunctionResponse.Details)
            {
                if (fisrtLayerMenu.Isdirectory == 0)
                {
                    foreach (var function in fisrtLayerMenu.Functions)
                    {
                        function.IsChecked = Sysrolemenufunctions.Any(t => t.Menuid == fisrtLayerMenu.Id && t.Functionid == function.Functionid);
                    }
                }
                foreach (var secondLayerMenu in fisrtLayerMenu.Subs)
                {
                    if (secondLayerMenu.Isdirectory == 0)
                    {
                        foreach (var function in secondLayerMenu.Functions)
                        {
                            function.IsChecked = Sysrolemenufunctions.Any(t => t.Menuid == secondLayerMenu.Id && t.Functionid == function.Functionid);
                        }
                    }
                    foreach (var thirdLayerMenu in secondLayerMenu.Subs)
                    {
                        if (thirdLayerMenu.Isdirectory == 0)
                        {
                            foreach (var function in thirdLayerMenu.Functions)
                            {
                                function.IsChecked = Sysrolemenufunctions.Any(t => t.Menuid == thirdLayerMenu.Id && t.Functionid == function.Functionid);
                            }
                        }
                    }
                }
            }

            return RickWebResult.Success(rolemenufunctionResponse);
        }

        /// <summary>
        /// 修改角色权限
        /// </summary>
        /// <param name="rolemenufunctionPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] RolemenufunctionPutRequest rolemenufunctionPutRequest)
        {
            await _roleService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            var oldSysrolemenufunctions = await _roleService.QueryAsync<Sysrolemenufunction>(t => t.Roleid == rolemenufunctionPutRequest.Id && t.Companyid == UserInfo.Companyid && t.Status == 1);
            List<Rolemenufunction> newSysmenuFunctionResponses = new List<Rolemenufunction>();

            foreach (var fisrtLayerMenu in rolemenufunctionPutRequest.Details)
            {
                if (fisrtLayerMenu.Isdirectory == 0)
                {
                    foreach (var function in fisrtLayerMenu.Functions)
                    {
                        if (function.IsChecked)
                        {
                            newSysmenuFunctionResponses.Add(new Rolemenufunction() { 
                                Menuid = fisrtLayerMenu.Id,
                                Functionid = function.Functionid
                            });
                        }
                    }
                }
                foreach (var secondLayerMenu in fisrtLayerMenu.Subs)
                {
                    if (secondLayerMenu.Isdirectory == 0)
                    {
                        foreach (var function in secondLayerMenu.Functions)
                        {
                            if (function.IsChecked)
                            {
                                newSysmenuFunctionResponses.Add(new Rolemenufunction()
                                {
                                    Menuid = secondLayerMenu.Id,
                                    Functionid = function.Functionid
                                });
                            }
                        }
                    }
                    foreach (var thirdLayerMenu in secondLayerMenu.Subs)
                    {
                        if (thirdLayerMenu.Isdirectory == 0)
                        {
                            foreach (var function in thirdLayerMenu.Functions)
                            {
                                if (function.IsChecked)
                                {
                                    newSysmenuFunctionResponses.Add(new Rolemenufunction()
                                    {
                                        Menuid = thirdLayerMenu.Id,
                                        Functionid = function.Functionid
                                    });
                                }
                            }
                        }
                    }
                }
            }

            //1、添加新的角色权限
            var newSysrolemenufunctions = newSysmenuFunctionResponses.Where(sysmenufunctionresponse => !oldSysrolemenufunctions.Any(t => t.Menuid == sysmenufunctionresponse.Menuid && t.Functionid == sysmenufunctionresponse.Functionid));
            foreach (var rolemenufunction in newSysrolemenufunctions)
            {
                Sysrolemenufunction sysrolemenufunction = new Sysrolemenufunction();
                sysrolemenufunction.Id = _idGenerator.NextId();
                sysrolemenufunction.Roleid = rolemenufunctionPutRequest.Id;
                sysrolemenufunction.Companyid = UserInfo.Companyid;
                sysrolemenufunction.Menuid = rolemenufunction.Menuid;
                sysrolemenufunction.Functionid = rolemenufunction.Functionid;

                sysrolemenufunction.Status = 1;
                sysrolemenufunction.Addtime = now;
                sysrolemenufunction.Lasttime = now;
                sysrolemenufunction.Adduser = UserInfo.Id;
                sysrolemenufunction.Lastuser = UserInfo.Id;
                await _roleService.AddAsync(sysrolemenufunction);
            }

            //2、删除旧的权限
            var deleteSysrolemenufunctions = oldSysrolemenufunctions.Where(oldSysrolemenufunction => !newSysmenuFunctionResponses.Any(t => t.Menuid == oldSysrolemenufunction.Menuid && t.Functionid == oldSysrolemenufunction.Functionid));
            foreach (var deleteSysrolemenufunction in deleteSysrolemenufunctions)
            {
                deleteSysrolemenufunction.Status = 0;
                deleteSysrolemenufunction.Lasttime = now;
                deleteSysrolemenufunction.Lastuser = UserInfo.Id;
                await _roleService.UpdateAsync(deleteSysrolemenufunction);
            }

            await _roleService.CommitAsync();
            _redisClientService.KeyDelete(ConstString.RickUserLoginKey);

            return RickWebResult.Success(new object());
        }

        public class RolemenufunctionResponse
        { 
            public long Id { get; set; }
            public string Name { get; set; }
            public List<SysmenuTreeResponse> Details { get; set; }
        }

        public class RolemenufunctionPutRequest
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<SysmenuTreeResponse> Details { get; set; }
        }
        public class Rolemenufunction
        {
            public long Menuid { get; set; }
            public long Functionid { get; set; }
            public string FunctionName { get; set; }
            public bool IsChecked { get; set; }
        }

    }
}
