using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Rick.RepositoryExpress.RedisService
{
    class RedisHelper
    {
        ConnectionMultiplexer redisConnection = null;
        int dataBaseNum;
        internal RedisHelper(string connectionString, int dbNum)
        {
            try
            {
                redisConnection = ConnectionMultiplexer.Connect(connectionString);
                dataBaseNum = dbNum;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                redisConnection = null;
            }
        }
        internal string StringGet(string key)
        {
            return redisConnection.GetDatabase(dataBaseNum).StringGet(key);
        }

        internal bool StringSet(string key, string value, TimeSpan? expiry = default(TimeSpan?))
        {
            return redisConnection.GetDatabase(dataBaseNum).StringSet(key, value, expiry);
        }
        internal long UserCodeGet()
        {
            return redisConnection.GetDatabase(dataBaseNum).HashIncrement("Code", "User");
        }

        internal async Task<bool> LockTakeAsync(string key,string value)
        {
            return await redisConnection.GetDatabase(dataBaseNum).LockTakeAsync(key, value, TimeSpan.FromSeconds(5));
        }


    }
}
