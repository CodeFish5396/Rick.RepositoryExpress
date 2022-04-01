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

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserchargeqrcodeimageController : RickControllerBase
    {
        private readonly ILogger<UserchargeqrcodeimageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IUserchargeqrcodeimageService _userchargeqrcodeimageService ;
        private readonly RedisClientService _redisClientService;

        public UserchargeqrcodeimageController(ILogger<UserchargeqrcodeimageController> logger, IUserchargeqrcodeimageService IUserchargeqrcodeimageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _userchargeqrcodeimageService = IUserchargeqrcodeimageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取支付二维码
        /// </summary>
        /// <param name="type"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserchargeqrcodeimageResponseList>> Get([FromQuery]int? type, [FromQuery]int? status)
        {
            IList<Userchargeqrcodeimage> userchargeqrcodeimages = await _userchargeqrcodeimageService.QueryAsync<Userchargeqrcodeimage>(t=>
            (!type.HasValue || t.Type == type) &&
            (!status.HasValue || t.Status == status)
            );
            UserchargeqrcodeimageResponseList userchargeqrcodeimageResponseList = new UserchargeqrcodeimageResponseList();
            userchargeqrcodeimageResponseList.Count = userchargeqrcodeimages.Count;
            userchargeqrcodeimageResponseList.List = userchargeqrcodeimages.Select(t => new UserchargeqrcodeimageResponse()
            {
                Id = t.Id,
                Imageid = t.Fileinfoid,
                Type = t.Type,
                Addtime = t.Addtime,
                Status = t.Status
            }).ToList();

            return RickWebResult.Success(userchargeqrcodeimageResponseList) ;
        }

        /// <summary>
        /// 提交二维码图片
        /// </summary>
        /// <param name="userchargeqrcodeimagePostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] UserchargeqrcodeimagePostRequest userchargeqrcodeimagePostRequest)
        {
            Userchargeqrcodeimage userchargeqrcodeimage = new Userchargeqrcodeimage();

            userchargeqrcodeimage.Id = _idGenerator.NextId();
            userchargeqrcodeimage.Fileinfoid = userchargeqrcodeimagePostRequest.Imageid;
            userchargeqrcodeimage.Type = userchargeqrcodeimagePostRequest.Type;
            userchargeqrcodeimage.Status = 1;
            userchargeqrcodeimage.Addtime = DateTime.Now;
            userchargeqrcodeimage.Adduser = UserInfo.Id;

            await _userchargeqrcodeimageService.AddAsync(userchargeqrcodeimage);

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 隐藏二维码
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            Userchargeqrcodeimage userchargeqrcodeimage = await _userchargeqrcodeimageService.FindAsync<Userchargeqrcodeimage>(id);
            if (userchargeqrcodeimage.Status == 1)
            {
                userchargeqrcodeimage.Status = 0;
                await _userchargeqrcodeimageService.UpdateAsync(userchargeqrcodeimage);
            }
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 激活二维码图片
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromQuery] long id)
        {
            Userchargeqrcodeimage userchargeqrcodeimage = await _userchargeqrcodeimageService.FindAsync<Userchargeqrcodeimage>(id);
            if (userchargeqrcodeimage.Status == 0)
            {
                userchargeqrcodeimage.Status = 1;
                await _userchargeqrcodeimageService.UpdateAsync(userchargeqrcodeimage);
            }
            return RickWebResult.Success(new object());
        }

        public class UserchargeqrcodeimageResponseList
        { 
            public List<UserchargeqrcodeimageResponse> List { get; set; }
            public int Count { get; set; }
        }

        public class UserchargeqrcodeimageResponse
        {
            public long Id { get; set; }
            public long Imageid { get; set; }
            public int Type { get; set; }
            public int Status { get; set; }
            public DateTime Addtime { get; set; }
        }
        public class UserchargeqrcodeimagePostRequest
        {
            public long Imageid { get; set; }
            public int Type { get; set; }

        }
    }
}
