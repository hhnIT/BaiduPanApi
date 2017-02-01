using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace BaiduPanApi
{
	public class CachedBaiduPanContext : BaiduPanContext
	{
		private const string CacheNamePrefix = "BaiduPanContext_";

		private MemoryCache cache;

		public TimeSpan CacheTtl { get; }

		public CachedBaiduPanContext(string username, string password, Func<byte[], string> captchaCallback, TimeSpan cacheTtl)
			: base(username, password, captchaCallback)
		{
			cache = new MemoryCache(CacheNamePrefix + username);
			CacheTtl = cacheTtl;
		}

		protected override async Task DisposeAsync()
		{
			await base.DisposeAsync();
			cache.Dispose();
		}

		private async Task<T> GetDataAsync<T>(string key, Func<Task<T>> valueFunc)
		{
			var value = new Lazy<Task<T>>(valueFunc);
			var res = cache.AddOrGetExisting(key, value, DateTimeOffset.Now + CacheTtl) as Lazy<Task<T>>;
			return await (res != null ? res.Value : value.Value);
		}

		private void RemoveCacheByPrefixes(params string[] prefixes)
		{
			foreach (var key in cache.Select(item => item.Key)
				.Where(key => prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
				cache.Remove(key);
		}

		public void ClearCache()
		{
			cache.Dispose();
			cache = new MemoryCache(CacheNamePrefix + Username);
		}

		public void ClearCache(string path) => RemoveCacheByPrefixes(path + "$");

		public override async Task<BaiduPanQuota> GetQuotaAsync()
			=> await GetDataAsync(nameof(GetQuotaAsync), base.GetQuotaAsync);
		
		public override async Task<IEnumerable<BaiduPanFileInformation>> ListDirectoryAsync(string path)
			=> await GetDataAsync($"{path}${nameof(ListDirectoryAsync)}", () => base.ListDirectoryAsync(path));

		public override async Task<BaiduPanFileInformation> GetItemInformationAsync(string path)
			=> await GetDataAsync($"{path}${nameof(GetItemInformationAsync)}", () => base.GetItemInformationAsync(path));

		public override async Task<IEnumerable<BaiduPanFileInformation>> SearchAsync(string path, string key, bool recursive)
			=> await base.SearchAsync(path, key, recursive);

		public override async Task DeleteItemAsync(string path)
		{
			await base.DeleteItemAsync(path);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}

		public override async Task CopyItemAsync(string path, string dest, string newName)
		{
			await base.CopyItemAsync(path, dest, newName);
			var newPath = $"{(dest == "/" ? string.Empty : dest)}/{newName}";
			RemoveCacheByPrefixes(dest + "$", newPath + "$", newPath + "/");
		}

		public override async Task MoveItemAsync(string path, string dest, string newName)
		{
			await base.MoveItemAsync(path, dest, newName);
			SplitPath(path, out var dir, out var name);
			var newPath = $"{(dest == "/" ? string.Empty : dest)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", dest + "$", newPath + "$", newPath + "/");
		}

		public override async Task RenameItemAsync(string path, string newName)
		{
			await base.RenameItemAsync(path, newName);
			SplitPath(path, out var dir, out var name);
			var newPath = $"{(dir == "/" ? string.Empty : dir)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", newPath + "$", newPath + "/");
		}

		public override async Task CreateDirectoryAsync(string path)
		{
			await base.CreateDirectoryAsync(path);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}

		public override async Task UploadFileAsync(string path, bool overwrite, HttpContent data)
		{
			await base.UploadFileAsync(path, overwrite, data);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "$");
		}

		public override async Task ConcatFileSlicesAsync(string path, bool overwrite, string[] slices)
		{
			await base.ConcatFileSlicesAsync(path, overwrite, slices);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "$");
		}
	}
}
