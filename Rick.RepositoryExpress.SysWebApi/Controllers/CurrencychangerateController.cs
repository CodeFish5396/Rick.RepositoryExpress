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
    /// 汇率
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencychangerateController : RickControllerBase
    {
        private readonly ILogger<CurrencychangerateController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ICurrencychangerateService _currencychangerateService;
        private readonly RedisClientService _redisClientService;

        public CurrencychangerateController(ILogger<CurrencychangerateController> logger, ICurrencychangerateService currencychangerateService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _currencychangerateService = currencychangerateService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询汇率
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<CurrencychangerateResponseList>> Get([FromQuery] int? status, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            CurrencychangerateResponseList currencychangerateResponseList = new CurrencychangerateResponseList();
            var results = from currencychangerate in _currencychangerateService.Query<Currencychangerate>(t => !status.HasValue || t.Status == status)
                          join sourceCurrency in _currencychangerateService.Query<Currency>()
                          on currencychangerate.Sourcecurrency equals sourceCurrency.Id
                          join targetCurrency in _currencychangerateService.Query<Currency>()
                          on currencychangerate.Targetcurrency equals targetCurrency.Id
                          select new CurrencychangerateResponse() {
                              Id = currencychangerate.Id,
                              Sourcecurrency = currencychangerate.Sourcecurrency,
                              Sourcecurrencyname = sourceCurrency.Name,
                              Targetcurrency = currencychangerate.Targetcurrency,
                              Targetcurrencyname = targetCurrency.Name,
                              Rate = currencychangerate.Rate,
                              Addtime = currencychangerate.Addtime,
                              Status = currencychangerate.Status
                          };

            currencychangerateResponseList.Count = await results.CountAsync();
            currencychangerateResponseList.List = await results.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();

            return RickWebResult.Success(currencychangerateResponseList);
        }

        /// <summary>
        /// 新增汇率
        /// </summary>
        /// <param name="currencychangerateRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody]CurrencychangerateRequest currencychangerateRequest)
        {
            await _currencychangerateService.BeginTransactionAsync();

            Currencychangerate oldCurrencychangerate = await _currencychangerateService.Query<Currencychangerate>(t=>t.Sourcecurrency == currencychangerateRequest.Sourcecurrency && t.Targetcurrency == currencychangerateRequest.Targetcurrency && t.Status == 1).SingleOrDefaultAsync();
            if (oldCurrencychangerate != null)
            {
                oldCurrencychangerate.Status = 0;
                await _currencychangerateService.UpdateAsync(oldCurrencychangerate);
            }
            Currencychangerate currencychangerate = new Currencychangerate();
            DateTime now = DateTime.Now;
            currencychangerate.Id = _idGenerator.NextId();
            currencychangerate.Sourcecurrency = currencychangerateRequest.Sourcecurrency;
            currencychangerate.Targetcurrency = currencychangerateRequest.Targetcurrency;
            currencychangerate.Rate = currencychangerateRequest.Rate;
            currencychangerate.Status = 1;
            currencychangerate.Addtime = now;
            currencychangerate.Lasttime = now;
            currencychangerate.Adduser = UserInfo.Id;
            currencychangerate.Lastuser = UserInfo.Id;
            await _currencychangerateService.AddAsync(currencychangerate);
            await _currencychangerateService.CommitAsync();

            return RickWebResult.Success(new object());
        }

        /// <summary>
        /// 修改汇率
        /// </summary>
        /// <param name="currencychangeratePutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<RickWebResult<object>> Put([FromBody] CurrencychangeratePutRequest currencychangeratePutRequest)
        {
            await _currencychangerateService.BeginTransactionAsync();

            Currencychangerate oldCurrencychangerate = await _currencychangerateService.FindAsync<Currencychangerate>(currencychangeratePutRequest.Id);
            if (oldCurrencychangerate != null)
            {
                oldCurrencychangerate.Status = 0;
                await _currencychangerateService.UpdateAsync(oldCurrencychangerate);
            }
            Currencychangerate currencychangerate = new Currencychangerate();
            DateTime now = DateTime.Now;
            currencychangerate.Id = _idGenerator.NextId();
            currencychangerate.Sourcecurrency = currencychangeratePutRequest.Sourcecurrency;
            currencychangerate.Targetcurrency = currencychangeratePutRequest.Targetcurrency;
            currencychangerate.Rate = currencychangeratePutRequest.Rate;
            currencychangerate.Status = 1;
            currencychangerate.Addtime = now;
            currencychangerate.Lasttime = now;
            currencychangerate.Adduser = UserInfo.Id;
            currencychangerate.Lastuser = UserInfo.Id;
            await _currencychangerateService.AddAsync(currencychangerate);
            await _currencychangerateService.CommitAsync();

            return RickWebResult.Success(new object());
        }
    }

    public class CurrencychangerateRequest
    {
        public long Sourcecurrency { get; set; }
        public long Targetcurrency { get; set; }
        public decimal Rate { get; set; }
    }

    public class CurrencychangeratePutRequest
    {
        public long Id { get; set; }
        public long Sourcecurrency { get; set; }
        public long Targetcurrency { get; set; }
        public decimal Rate { get; set; }
    }

    public class CurrencychangerateResponseList
    { 
        public int Count { get; set; }
        public List<CurrencychangerateResponse> List { get; set; }
    }

    public class CurrencychangerateResponse
    {
        public long Id { get; set; }
        public long Sourcecurrency { get; set; }
        public string Sourcecurrencyname { get; set; }
        public long Targetcurrency { get; set; }
        public string Targetcurrencyname { get; set; }
        public decimal Rate { get; set; }
        public DateTime Addtime { get; set; }
        public int Status { get; set; }

    }

}
