using System;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.RedisService
{
    public class RedisClientService
    {
        RedisHelper redisHelper;
        public RedisClientService(string connectionString, int dbNum)
        {
            redisHelper = new RedisHelper(connectionString, dbNum);
        }
        public long UserCodeGet()
        {
            return redisHelper.UserCodeGet();
        }
        public bool StringSet(string key, string value, TimeSpan? expiry = default(TimeSpan?))
        {
            return redisHelper.StringSet(key, value, expiry);
        }
        public string StringGet(string key)
        {
            return redisHelper.StringGet(key);
        }
        public bool HashSet(string key, string hashkey, string value)
        {
            return redisHelper.HashSet(key, hashkey, value);
        }

        public string HashGet(string key, string hashkey)
        {
            return redisHelper.HashGet(key, hashkey);
        }
        public bool KeyDelete(string key)
        {
            return redisHelper.KeyDelete(key);
        }

        public async Task<bool> LockTakeAsync(string key, string value)
        {
            return await redisHelper.LockTakeAsync(key, value);
        }

    }
}
