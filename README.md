# DoubleCache
A [cache-aside](https://msdn.microsoft.com/en-us/library/dn589799.aspx) implementation using Redis combined with an in-memory cache. 

[Redis](https://github.com/antirez/redis) is a fast, distributed key value store and then some. Using a remote store such as Redis adds two extra costs, IO and serialization. Having a local in-memory object store eliminates these two factors, at the cost of having a local cache on each client. Besides memory management, having multiple cache instances often result in synchronization issues. 

DoubleCache provides synchronized local caches using Redis pub/sub combined with Redis key/value as a fallback if the local cache is empty.  

###Azure Managed Cache retirement
Having a local cache in front of a sentralized cache is nothing new, it's a feature available to the users of [Azure Managed Cache](https://msdn.microsoft.com/en-us/library/azure/dn386096.aspx). On December 3rd 2015, Microsoft announced the [retirement of Azure Managed Cache](https://azure.microsoft.com/en-us/blog/azure-managed-cache-and-in-role-cache-services-to-be-retired-on-11-30-2016/), the migrate path is to use the Azure Redis cache offering. As Microsoft has not made their own client for Redis, they recommend using the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) client. This client does not provide a local cache feature and the migration document states 
>Local cache: Client applications would need to implement this functionality using a dictionary or similar data structure.

DoubleCache provides this functionality using System.Runtime.Cache.MemoryCache. By creating your own implementation of the ICacheAside interface, it is easy to replace the local or remote cache with your own. 

##Usage
Add a reference to DoubleCache (nuget coming shortly) and initialize the DoubleCache with a remote and a local cache. 
```
var connection = ConnectionMultiplexer.Connect("localhost");
var serializer = new MsgPackItemSerializer();
var remoteCache = new RedisCache(connection.GetDatabase(), serializer);
var _pubSubCache = new DoubleCache.DoubleCache(
  new SubscribingCache(
    new DoubleCache.LocalCache.MemCache(), 
    new RedisSubscriber(connection, remoteCache, serializer)),
  new PublishingCache(
    remoteCache, 
    new RedisPublisher(connection, serializer))); 
```
The sample above assumes the local cache must be kept in sync with the remote cache. If sync is not required, the cache can be created without the pub/sub option:

```
var connection = ConnectionMultiplexer.Connect("localhost");
 _doubleCache = new DoubleCache.DoubleCache(
  new DoubleCache.LocalCache.MemCache(),
  new RedisCache(connection.GetDatabase(), new MsgPackItemSerializer()));
```

To use the initialized cache, ca

##Implementation
The ICacheAside interface is the main part of DoubleCache, all variants relies on implementations of this single interface. 
```
public interface ICacheAside
  {
    void Add<T>(string key, T item);
    
    Task<T> GetAsync<T>(string key, Func<Task<T>> dataRetriever) where T : class;
    Task<object> GetAsync(string key, Type type, Func<Task<object>> dataRetriever);
  }
```
DoubleCache comes with the following implementations of this interface
* LocalCache.MemCache - using System.Runtime.Memory
* Redis.RedisCache - using StackExchange.Redis client
* SubscribingCache - a decorator supporting push notifications of cache updates
* PublishingCache - a decorator publishing cache changes
* DoubleCache - An implementation using a local and remote cache


Depending on your cache need, you can combine these implementations and decorators as you need. The most complete example would be a DoubleCache which takes a local cache decorated with a SubscribingCache and a RedisCache decorated with a PublishingCache. This will result in a local cache that will be in sync with the other local caches; if a value isn't found the value will be retrieved from Redis before ultimately being resolved using the func provided to the cache.