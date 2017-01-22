using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace BaiduPanApi
{
	public class CachedBaiduPanContext : BaiduPanContext
	{
		private const string CacheNamePrefix = "BaiduPanContext_";

		private MemoryCache cache;

		public TimeSpan CacheTtl { get; }

		public CachedBaiduPanContext(string username, string password, TimeSpan cacheTtl) : base(username, password)
		{
			cache = new MemoryCache(CacheNamePrefix + username);
			CacheTtl = cacheTtl;
		}

		private T GetData<T>(string key, Func<T> valueFunc)
		{
			var value = new Lazy<T>(valueFunc);
			var res = cache.AddOrGetExisting(key, value, DateTimeOffset.Now + CacheTtl) as Lazy<T>;
			return res != null ? res.Value : value.Value;
		}

		private void RemoveCacheByPrefixes(params string[] prefixes)
		{
			foreach (var key in cache.Select(item => item.Key)
				.Where(key => prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
				cache.Remove(key);
		}

		public void RefreshCache()
		{
			cache.Dispose();
			cache = new MemoryCache(CacheNamePrefix + Username);
		}

		public override IEnumerable<BaiduPanFileInformation> ListDirectory(string path)
			=> GetData($"{path}${nameof(ListDirectory)}", () => base.ListDirectory(path));

		public override BaiduPanFileInformation GetItemInformation(string path)
			=> GetData($"{path}${nameof(GetItemInformation)}", () => base.GetItemInformation(path));

		public override IEnumerable<BaiduPanFileInformation> Search(string path, string key, bool recursive)
			=> base.Search(path, key, recursive);

		public override BaiduPanQuota GetQuota()
			=> GetData(nameof(GetQuota), base.GetQuota);

		public override void DeleteItem(string path)
		{
			base.DeleteItem(path);
			(var dir, var name) = SplitPath(path);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}

		public override void MoveItem(string path, string dest, string newName)
		{
			base.MoveItem(path, dest, newName);
			(var dir, var name) = SplitPath(path);
			var newPath = $"{(dest == "/" ? string.Empty : dest)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", dest + "$", newPath + "$", newPath + "/");
		}

		public override void RenameItem(string path, string newName)
		{
			base.RenameItem(path, newName);
			(var dir, var name) = SplitPath(path);
			var newPath = $"{(dir == "/" ? string.Empty : dir)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", newPath + "$", newPath + "/");
		}

		public override void CreateDirectory(string path)
		{
			base.CreateDirectory(path);
			(var dir, var name) = SplitPath(path);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}
	}
}
