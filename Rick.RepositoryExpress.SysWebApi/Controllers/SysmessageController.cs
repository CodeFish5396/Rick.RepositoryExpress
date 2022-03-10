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
using System.IO;
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SysmessageController : RickControllerBase
    {
        private readonly ILogger<SysmessageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IMessageService _messageService;
        private readonly RedisClientService _redisClientService;

        public SysmessageController(ILogger<SysmessageController> logger, IMessageService messageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _messageService = messageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取系统消息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<List<Message>>> Get([FromQuery] int pageSize = 10)
        {
            await _messageService.BeginTransactionAsync();

            string RoleMenuFunctionInfosCache = _redisClientService.HashGet(ConstString.RickRoleMenuFunctionInfosKey, UserInfo.Id.ToString());
            List<RoleMenuFunctionInfo> roleMenuFunctionInfos = JsonConvert.DeserializeObject<List<RoleMenuFunctionInfo>>(RoleMenuFunctionInfosCache);
            List<string> menus = (from menu in roleMenuFunctionInfos
                                  select menu.Menuindex).Distinct().ToList();

            var messageconsumes = from messageconsume in _messageService.Query<Messageconsume>(t => t.Sysuser == UserInfo.Id)
                                  select messageconsume.Messageid;

            var messages = await (from message in _messageService.Query<Message>(t => true)
                                  where !messageconsumes.Contains(message.Id)
                                  && menus.Contains(message.Index)
                                  select message).OrderBy(t => t.Id).Take(pageSize).ToListAsync();

            foreach (var message in messages)
            {
                Messageconsume messageconsume = new Messageconsume();
                messageconsume.Id = _idGenerator.NextId();
                messageconsume.Sysuser = UserInfo.Id;
                messageconsume.Addtime = DateTime.Now;
                messageconsume.Adduser = UserInfo.Id;
                messageconsume.Messageid = message.Id;
                await _messageService.AddAsync(messageconsume);
            }

            await _messageService.CommitAsync();

            return RickWebResult.Success(messages);
        }

    }
}
