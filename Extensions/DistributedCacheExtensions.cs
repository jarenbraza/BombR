using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BombermanAspNet.Extensions
{
	public static class DistributedCacheExtensions
	{
		public static async Task SetRecordAsync<T>(
			this IDistributedCache cache,
			string recordId,
			T value,
			TimeSpan? absoluteExpiration = null,
			TimeSpan? slidingExpiration = null)
		{
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(30),
				SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(5)
			};

			var jsonValue = JsonSerializer.Serialize(value);

			await cache.SetStringAsync(recordId, jsonValue, options);
		}

		public static async Task<T> GetRecordAsync<T>(
			this IDistributedCache cache,
			string recordId)
		{
			var jsonValue = await cache.GetStringAsync(recordId);

			if (jsonValue == null)
			{
				return default;
			}

			return JsonSerializer.Deserialize<T>(jsonValue);
		}
	}
}
