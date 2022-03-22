using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace WebSocketsTutorial.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketsController : ControllerBase
    {
        private readonly ILogger<WebSocketsController> _logger;
        private readonly IMessageService _messageService;
        private static ConcurrentDictionary<long, WebSocket> ConnetedWebsockets = new ConcurrentDictionary<long, WebSocket>();
        private static object lockKey = new object();
        private readonly RedisClientService _redisClientService;
        private readonly IIdGeneratorService _idGenerator;


        public WebSocketsController(ILogger<WebSocketsController> logger, IMessageService messageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _messageService = messageService;
            _idGenerator = idGenerator;

            _redisClientService = redisClientService;
        }

        [HttpGet]
        public async Task Get([FromQuery] string token)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                UserInfo userInfo = AuthTokenHelper.Get(token);
                if (userInfo == null || userInfo.Id <= 0)
                {
                    return;
                }
                else
                {
                    string cachedToken = _redisClientService.HashGet(ConstString.RickUserLoginKey, userInfo.Id.ToString());
                    if (string.IsNullOrEmpty(cachedToken) || cachedToken != token)
                    {
                        HttpContext.Response.StatusCode = 401;
                    }
                    else
                    {
                        if (ConnetedWebsockets.ContainsKey(userInfo.Id))
                        {
                            await ConnetedWebsockets[userInfo.Id].CloseAsync(WebSocketCloseStatus.NormalClosure, "异地登录", CancellationToken.None);
                            WebSocket remWebSocket;
                            ConnetedWebsockets.TryRemove(userInfo.Id, out remWebSocket);
                        }
                        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                        ConnetedWebsockets.TryAdd(userInfo.Id, webSocket);
                        _logger.Log(LogLevel.Information, "WebSocket connection established");
                        await Echo(webSocket, userInfo.Id, userInfo);
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = 401;
            }
        }

        private async Task Echo(WebSocket webSocket, long userId, UserInfo userInfo)
        {
            List<string> menus = new List<string>();
            if (!userInfo.IsDefaultRole)
            {
                string RoleMenuFunctionInfosCache = _redisClientService.HashGet(ConstString.RickRoleMenuFunctionInfosKey, userInfo.Id.ToString());
                List<RoleMenuFunctionInfo> roleMenuFunctionInfos = JsonConvert.DeserializeObject<List<RoleMenuFunctionInfo>>(RoleMenuFunctionInfosCache);
                menus = (from menu in roleMenuFunctionInfos
                         select menu.Menuindex).Distinct().ToList();
            }
            while (!webSocket.CloseStatus.HasValue)
            {
                var messageconsumes = from messageconsume in _messageService.Query<Messageconsume>(t => t.Sysuser == userId)
                                      select messageconsume.Messageid;

                var messages = await (from message in _messageService.Query<Message>(t => true)
                                      where !messageconsumes.Contains(message.Id)
                                      && (userInfo.IsDefaultRole || menus.Contains(message.Index))
                                      select message).ToListAsync();

                foreach (var message in messages)
                {
                    var serverMsg = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(message));

                    await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    Messageconsume messageconsume = new Messageconsume();
                    messageconsume.Id = _idGenerator.NextId();
                    messageconsume.Sysuser = userId;
                    messageconsume.Addtime = DateTime.Now;
                    messageconsume.Adduser = userId;
                    messageconsume.Messageid = message.Id;
                    await _messageService.AddAsync(messageconsume);
                    _logger.Log(LogLevel.Information, "WebSocket Send Message");

                    await Task.Delay(1000 * 10);
                }
                await Task.Delay(1000 * 10);
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信结束", CancellationToken.None);
            WebSocket remWebSocket;
            ConnetedWebsockets.TryRemove(userId, out remWebSocket);

            _logger.Log(LogLevel.Information, "WebSocket connection closed");
        }
    }
}