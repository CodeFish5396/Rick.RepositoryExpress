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

        internal bool HashSet(string key, string hashkey, string value)
        {
            return redisConnection.GetDatabase(dataBaseNum).HashSet(key, hashkey, value);
        }
        internal string HashGet(string key, string hashkey)
        {
            return redisConnection.GetDatabase(dataBaseNum).HashGet(key, hashkey);
        }

        internal bool KeyDelete(string key)
        {
            return redisConnection.GetDatabase(dataBaseNum).KeyDelete(key);
        }


        internal long UserCodeGet()
        {
            return redisConnection.GetDatabase(dataBaseNum).HashIncrement("Code", "User");
        }

        internal string PackageCodeGet()
        {
            string date = DateTime.Now.ToString("ddHHmmssfff");
            string current = redisConnection.GetDatabase(dataBaseNum).HashIncrement("Code", "Package" + date).ToString();

            string packagecode = "00" + current;
            packagecode = packagecode.Substring(packagecode.Length - 2);

            string result = "DR" + date + packagecode + "GJ";
            return result;
        }
        internal string OrderCodeGet()
        {
            string date = DateTime.Now.ToString("ddHHmmssfff");
            string current = redisConnection.GetDatabase(dataBaseNum).HashIncrement("Code", "Order" + date).ToString();
            string ordercode = "00" + current;
            ordercode = ordercode.Substring(ordercode.Length - 2);
            string result = "DR" + date + ordercode;
            return result;
        }
        internal async Task<bool> LockTakeAsync(string key,string value)
        {
            return await redisConnection.GetDatabase(dataBaseNum).LockTakeAsync(key, value, TimeSpan.FromSeconds(5));
        }


    }
}
