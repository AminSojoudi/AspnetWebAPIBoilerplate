using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebapiBoilerplate.Database
{
    public class RedisDatabase
    {
        private static ConnectionMultiplexer redis;


        public RedisDatabase()
        {
            redis = GetConnectionMultiplexer();
        }


        public ConnectionMultiplexer GetConnectionMultiplexer()
        {
            if (redis == null)
            {
                redis = ConnectionMultiplexer.Connect("localhost");
            }
            return redis;
        }


        public static void SetUserConnectionID(int userId , string connectionId)
        {
            redis.GetDatabase().StringSet(userId.ToString() , connectionId);
        }

        public static string GetConnectionID(int userId)
        {
            return redis.GetDatabase().StringGet(userId.ToString());
        }

        public static void StringSet(string item)
        {
            redis.GetDatabase().StringSet(item, "valueIsNotImportant", TimeSpan.FromMilliseconds(30000));
        }

        public static bool SetContains(string item)
        {
            RedisValue value = redis.GetDatabase().StringGet(item);
            if (value.IsNullOrEmpty)
            {
                return false;
            }
            else
            {
                return true;
            }
        }



    }
}