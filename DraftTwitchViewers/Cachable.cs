using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DraftTwitchViewers
{
    public class Cacheable<T>
    {
        public T Data;
        public DateTime CachedAt;
        public double CacheSeconds;

        public Cacheable(T data, double cacheSeconds)
        {
            Data = data;
            CachedAt = DateTime.UtcNow;
            CacheSeconds = cacheSeconds;
        }
    }
}
