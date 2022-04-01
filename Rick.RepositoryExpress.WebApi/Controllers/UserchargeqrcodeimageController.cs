using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserchargeqrcodeimageController : RickControllerBase
    {
        private readonly ILogger<UserchargeqrcodeimageController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IUserchargeqrcodeimageService _userchargeqrcodeimageService;
        private readonly RedisClientService _redisClientService;

        public UserchargeqrcodeimageController(ILogger<UserchargeqrcodeimageController> logger, IUserchargeqrcodeimageService userchargeqrcodeimageService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _userchargeqrcodeimageService = userchargeqrcodeimageService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取二维码图片
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserchargeqrcodeimageResponse>> Get()
        {
            IList<Userchargeqrcodeimage> userchargeqrcodeimages = await _userchargeqrcodeimageService.QueryAsync<Userchargeqrcodeimage>(t => t.Status == 1);
            UserchargeqrcodeimageResponse userchargeqrcodeimageResponse = new UserchargeqrcodeimageResponse();
            userchargeqrcodeimageResponse.Wechatpay = userchargeqrcodeimages.Where(t => t.Type == 1).Select(t => t.Fileinfoid).ToList();
            userchargeqrcodeimageResponse.Alipay = userchargeqrcodeimages.Where(t => t.Type == 2).Select(t => t.Fileinfoid).ToList();
            return RickWebResult.Success(userchargeqrcodeimageResponse);
        }
        public class UserchargeqrcodeimageResponse
        {
            public List<long> Wechatpay { get; set; }
            public List<long> Alipay { get; set; }
        }

    }
}
