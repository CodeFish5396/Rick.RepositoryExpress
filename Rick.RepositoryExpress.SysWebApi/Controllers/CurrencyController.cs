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
    /// 货币
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyController : RickControllerBase
    {
        private readonly ILogger<CurrencyController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ICurrencyService _currencyService;
        private readonly RedisClientService _redisClientService;

        public CurrencyController(ILogger<CurrencyController> logger, ICurrencyService currencyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _currencyService = currencyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询货币
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<IEnumerable<CurrencyResponse>>> Get([FromQuery]int? status)
        {
            var results = await _currencyService.QueryAsync<Currency>(t => !status.HasValue || t.Status == status);
            return RickWebResult.Success(results.Select(t => new CurrencyResponse()
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Status = t.Status
            }));
        }

        /// <summary>
        /// 创建货币
        /// </summary>
        /// <param name="currencyRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<CurrencyResponse>> Post([FromBody] CurrencyRequest currencyRequest)
        {
            await _currencyService.BeginTransactionAsync();

            Currency currency = new Currency();
            DateTime now = DateTime.Now;
            currency.Id = _idGenerator.NextId();
            currency.Name = currencyRequest.Name;
            currency.Code = currencyRequest.Code;
            currency.Status = 1;
            currency.Addtime = now;
            currency.Lasttime = now;
            currency.Adduser = UserInfo.Id;
            currency.Lastuser = UserInfo.Id;
            await _currencyService.AddAsync(currency);
            await _currencyService.CommitAsync();
            CurrencyResponse currencyResponse = new CurrencyResponse();
            currencyResponse.Id = currency.Id;
            currencyResponse.Code = currency.Code;
            currencyResponse.Name = currency.Name;
            return RickWebResult.Success(currencyResponse);

        }

        /// <summary>
        /// 修改货币
        /// </summary>
        /// <param name="currencyPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<CurrencyResponse>> Put([FromBody] CurrencyPutRequest currencyPutRequest)
        {
            await _currencyService.BeginTransactionAsync();
            Currency currency = await _currencyService.FindAsync<Currency>(currencyPutRequest.Id);
            DateTime now = DateTime.Now;
            currency.Name = currencyPutRequest.Name;
            currency.Code = currencyPutRequest.Code;
            currency.Status = 1;
            currency.Lasttime = now;
            currency.Lastuser = UserInfo.Id;

            await _currencyService.UpdateAsync(currency);
            await _currencyService.CommitAsync();
            CurrencyResponse currencyResponse = new CurrencyResponse();
            currencyResponse.Id = currency.Id;
            currencyResponse.Code = currency.Code;
            currencyResponse.Name = currency.Name;
            return RickWebResult.Success(currencyResponse);
        }
        
        /// <summary>
        /// 删除货币
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<RickWebResult<CurrencyResponse>> Delete([FromQuery] long id)
        {
            await _currencyService.BeginTransactionAsync();
            Currency currency = await _currencyService.FindAsync<Currency>(id);
            DateTime now = DateTime.Now;
            currency.Status =0;
            currency.Lasttime = now;
            currency.Lastuser = UserInfo.Id;

            await _currencyService.UpdateAsync(currency);
            await _currencyService.CommitAsync();
            CurrencyResponse currencyResponse = new CurrencyResponse();
            currencyResponse.Id = currency.Id;
            currencyResponse.Code = currency.Code;
            currencyResponse.Name = currency.Name;
            return RickWebResult.Success(currencyResponse);
        }

    }

    public class CurrencyRequest
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class CurrencyPutRequest
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class CurrencyResponse
    {
        public long Id { get; set; }
        public string Code { get; set; }

        public string Name { get; set; }
        public int Status { get; set; }

    }

}
