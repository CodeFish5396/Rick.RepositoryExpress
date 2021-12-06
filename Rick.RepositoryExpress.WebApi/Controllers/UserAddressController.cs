using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAddressController : RickControllerBase
    {
        private readonly ILogger<UserAddressController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraddressService _appuseraddressService;
        private readonly RedisClientService _redisClientService;

        public UserAddressController(ILogger<UserAddressController> logger, IAppuseraddressService appuseraddressService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraddressService = appuseraddressService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 创建地址
        /// </summary>
        /// <param name="userAddressRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<UserAddressResponse>> Post([FromBody] UserAddressRequest userAddressRequest)
        {
            await _appuseraddressService.BeginTransactionAsync();
            Appuseraddress appuseraddress = new Appuseraddress();
            appuseraddress.Appuser = UserInfo.Id;
            appuseraddress.Id = _idGenerator.NextId();
            appuseraddress.Nationid = userAddressRequest.Nationid;
            appuseraddress.Name = userAddressRequest.Name;
            appuseraddress.Contactnumber = userAddressRequest.Contactnumber;
            appuseraddress.Region = userAddressRequest.Region;
            appuseraddress.Address = userAddressRequest.Address;
            appuseraddress.Weight = userAddressRequest.Weight;
            appuseraddress.Status = 1;
            appuseraddress.Adduser = UserInfo.Id;
            appuseraddress.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            appuseraddress.Addtime = now;
            appuseraddress.Lasttime = now;
            await _appuseraddressService.AddAsync(appuseraddress);
            await _appuseraddressService.CommitAsync();
            UserAddressResponse userAddressResponse = new UserAddressResponse();
            userAddressResponse.Id = appuseraddress.Id;
            userAddressResponse.Nationid = appuseraddress.Nationid;
            userAddressResponse.Name = appuseraddress.Name;
            userAddressResponse.Contactnumber = appuseraddress.Contactnumber;
            userAddressResponse.Region = appuseraddress.Region;
            userAddressResponse.Address = appuseraddress.Address;
            userAddressResponse.Weight = appuseraddress.Weight;

            return RickWebResult.Success(userAddressResponse);
        }

        /// <summary>
        /// 获取地址
        /// </summary>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<UserAddressResponse>>> Get([FromQuery] bool isDefault)
        {
            var results = await _appuseraddressService.Query<Appuseraddress>(t => t.Status == 1 && t.Appuser == UserInfo.Id)
                .OrderByDescending(t => t.Weight).ThenByDescending(t => t.Lasttime).ToListAsync();
            IEnumerable<UserAddressResponse> userAddressResponses;
            if (isDefault)
            {
                userAddressResponses = Enumerable.Repeat(results.FirstOrDefault(), 1).Select(address => new UserAddressResponse
                {
                    Id = address.Id,
                    Nationid = address.Nationid,
                    Name = address.Name,
                    Contactnumber = address.Contactnumber,
                    Region = address.Region,
                    Address = address.Address,
                    Weight = address.Weight
                });
            }
            else
            {
                userAddressResponses = results.Select(address => new UserAddressResponse
                {
                    Id = address.Id,
                    Nationid = address.Nationid,
                    Name = address.Name,
                    Contactnumber = address.Contactnumber,
                    Region = address.Region,
                    Address = address.Address,
                    Weight = address.Weight
                });
            }
            var nationids = userAddressResponses.Select(t => t.Nationid);
            var nations = await _appuseraddressService.Query<Nation>(t => nationids.Contains(t.Id)).ToListAsync();
            foreach (var res in userAddressResponses)
            {
                res.NationName = nations.FirstOrDefault(t => t.Id == res.Nationid).Name;
            }
            return RickWebResult.Success(userAddressResponses);

        }

        /// <summary>
        /// 删除用户地址
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<object>> Delete([FromQuery] long id)
        {
            await _appuseraddressService.BeginTransactionAsync();
            Appuseraddress appuseraddress = await _appuseraddressService.FindAsync<Appuseraddress>(id);
            appuseraddress.Status = 0;
            appuseraddress.Lasttime = DateTime.Now;
            appuseraddress.Lastuser = UserInfo.Id;
            await _appuseraddressService.UpdateAsync(appuseraddress);
            await _appuseraddressService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改用户地址
        /// </summary>
        /// <param name="userAddressPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<UserAddressResponse>> Put([FromBody] UserAddressPutRequest userAddressPutRequest)
        {
            await _appuseraddressService.BeginTransactionAsync();
            Appuseraddress appuseraddress = await _appuseraddressService.FindAsync<Appuseraddress>(userAddressPutRequest.Id);
            appuseraddress.Status = 0;
            appuseraddress.Lasttime = DateTime.Now;
            appuseraddress.Lastuser = UserInfo.Id;

            await _appuseraddressService.UpdateAsync(appuseraddress);

            Appuseraddress appuseraddressPut = new Appuseraddress();
            appuseraddressPut.Id = _idGenerator.NextId();
            appuseraddressPut.Appuser = UserInfo.Id;

            appuseraddressPut.Nationid = userAddressPutRequest.Nationid;
            appuseraddressPut.Name = userAddressPutRequest.Name;
            appuseraddressPut.Contactnumber = userAddressPutRequest.Contactnumber;
            appuseraddressPut.Region = userAddressPutRequest.Region;
            appuseraddressPut.Address = userAddressPutRequest.Address;
            appuseraddressPut.Weight = userAddressPutRequest.Weight;
            appuseraddressPut.Status = 1;
            appuseraddressPut.Adduser = UserInfo.Id;
            appuseraddressPut.Lastuser = UserInfo.Id;
            DateTime now = DateTime.Now;
            appuseraddressPut.Addtime = now;
            appuseraddressPut.Lasttime = now;
            await _appuseraddressService.AddAsync(appuseraddressPut);
            var nation = await _appuseraddressService.FindAsync<Nation>(appuseraddressPut.Nationid);


            UserAddressResponse userAddressResponse = new UserAddressResponse();
            userAddressResponse.Id = appuseraddressPut.Id;
            userAddressResponse.Nationid = appuseraddressPut.Nationid;
            userAddressResponse.Name = appuseraddressPut.Name;
            userAddressResponse.Contactnumber = appuseraddressPut.Contactnumber;
            userAddressResponse.Region = appuseraddressPut.Region;
            userAddressResponse.Address = appuseraddressPut.Address;
            userAddressResponse.Weight = appuseraddressPut.Weight;
            userAddressResponse.NationName = nation.Name;
            await _appuseraddressService.CommitAsync();
            return RickWebResult.Success(userAddressResponse);
        }

        public class UserAddressRequest
        {
            public long Nationid { get; set; }
            public string Name { get; set; }
            public string Contactnumber { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
            public int Weight { get; set; }

        }
        public class UserAddressPutRequest
        {
            public long Id { get; set; }
            public long Nationid { get; set; }
            public string Name { get; set; }
            public string Contactnumber { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
            public int Weight { get; set; }
        }

        public class UserAddressResponse
        {
            public long Id { get; set; }
            public int Weight { get; set; }
            public long Nationid { get; set; }
            public string NationName { get; set; }

            public string Name { get; set; }
            public string Contactnumber { get; set; }
            public string Region { get; set; }
            public string Address { get; set; }
        }

    }
}
