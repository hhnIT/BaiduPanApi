using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace BaiduPanApi
{
	/// <summary>
	/// Represents a logged in session of BaiduPan with caching enabled.
	/// </summary>
	/// <remarks>
	/// <para>Use this class to manipulate the BaiduPan assosiated with the logged in account.</para>
	/// <para>Caching is enabled with <see cref="MemoryCache" />.</para>
	/// <para>All paths passed in should <c>/</c> as the delimiter and should start with a <c>/</c>.</para>
	/// </remarks>
	public class CachedBaiduPanContext : BaiduPanContext
	{
		private const string CacheNamePrefix = "BaiduPanContext_";

		private MemoryCache cache;

		/// <summary>
		/// Gets the TTL of the cache entries.
		/// </summary>
		/// <value>The TTL of the cache entries.</value>
		/// <remarks>
		/// A cache entry will be kept for the time of <see cref="CacheTtl" /> before expiration.
		/// </remarks>
		public TimeSpan CacheTtl { get; }

		/// <summary>
		/// Creates an instance of <see cref="CachedBaiduPanContext" />.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <param name="captchaCallback">The callback that will be invoked when a captcha is required.</param>
		/// <param name="cacheTtl">The TTL of the cache entries.</param>
		/// <remarks>
		/// <para>
		/// When a captcha is required by the Baidu servers, <paramref name="captchaCallback" /> will be invoked
		/// with the binary data of the captcha image, it should then return the text in the captcha image.
		/// </para>
		/// <para>See <see cref="CacheTtl" /> for information about <paramref name="cacheTtl" />.</para>
		/// </remarks>
		public CachedBaiduPanContext(string username, string password, Func<byte[], string> captchaCallback, TimeSpan cacheTtl)
			: base(username, password, captchaCallback)
		{
			cache = new MemoryCache(CacheNamePrefix + username);
			CacheTtl = cacheTtl;
		}

		/// <summary>
		/// An internal helper for calling async methods in <see cref="BaiduPanContext.Dispose" />.
		/// </summary>
		/// <remarks>
		/// <para>This method will be called in <see cref="BaiduPanContext.Dispose" />.</para>
		/// <para>Override this method to release resources in <see cref="BaiduPanContext.Dispose" />.</para>
		/// </remarks>
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

		/// <summary>
		/// Clear all cache entries.
		/// </summary>
		public void ClearCache()
		{
			cache.Dispose();
			cache = new MemoryCache(CacheNamePrefix + Username);
		}

		/// <summary>
		/// Clear all cache entries with specified path.
		/// </summary>
		public void ClearCache(string path) => RemoveCacheByPrefixes(path + "$");

		/// <summary>
		/// Gets quota information of the BaiduPan.
		/// </summary>
		/// <returns>Quota information of the BaiduPan.</returns>
		/// <remarks>The result of this method is cached.</remarks>
		public override async Task<BaiduPanQuota> GetQuotaAsync()
			=> await GetDataAsync(nameof(GetQuotaAsync), base.GetQuotaAsync);

		/// <summary>
		/// Gets all files and directories contained in a directory.
		/// </summary>
		/// <param name="path">The directory to list.</param>
		/// <returns>All files and directories contained in <paramref name="path" />.</returns>
		/// <remarks>The result of this method is cached.</remarks>
		public override async Task<IEnumerable<BaiduPanFileInformation>> ListDirectoryAsync(string path)
			=> await GetDataAsync($"{path}${nameof(ListDirectoryAsync)}", () => base.ListDirectoryAsync(path));

		/// <summary>
		/// Gets information about a file or a directory.
		/// </summary>
		/// <param name="path">The directory to get information about.</param>
		/// <returns>Information about <paramref name="path" />.</returns>
		/// <remarks>The result of this method is cached.</remarks>
		public override async Task<BaiduPanFileInformation> GetItemInformationAsync(string path)
			=> await GetDataAsync($"{path}${nameof(GetItemInformationAsync)}", () => base.GetItemInformationAsync(path));

		/// <summary>
		/// Searches files and directories in a directory.
		/// </summary>
		/// <param name="path">The directory to search inside.</param>
		/// <param name="key">The keyword to search.</param>
		/// <param name="recursive">Specifies whether to search also in subdirectories.</param>
		/// <returns>Information about all of the matched files and directories.</returns>
		/// <remarks>
		/// <para>This method won't throw exceptions even when <paramref name="path" /> doesn't exist.</para>
		/// <para>The result of this method is cached.</para>
		/// </remarks>
		public override async Task<IEnumerable<BaiduPanFileInformation>> SearchAsync(string path, string key, bool recursive)
			=> await base.SearchAsync(path, key, recursive);

		/// <summary>
		/// Deletes a file or a directory.
		/// </summary>
		/// <param name="path">The file or directory to delete.</param>
		/// <remarks>
		/// <para>All cache entries related will be cleared.</para>
		/// <para>This method won't throw exceptions even when <paramref name="path" /> doesn't exist.</para>
		/// </remarks>
		public override async Task DeleteItemAsync(string path)
		{
			await base.DeleteItemAsync(path);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}

		/// <summary>
		/// Copys a file or a directory.
		/// </summary>
		/// <param name="path">The file or directory to copy.</param>
		/// <param name="dest">The destination directory to copy to.</param>
		/// <param name="newName">The name of the new file or directory.</param>
		/// <remarks>All cache entries related will be cleared.</remarks>
		public override async Task CopyItemAsync(string path, string dest, string newName)
		{
			await base.CopyItemAsync(path, dest, newName);
			var newPath = $"{(dest == "/" ? string.Empty : dest)}/{newName}";
			RemoveCacheByPrefixes(dest + "$", newPath + "$", newPath + "/");
		}

		/// <summary>
		/// Moves a file or a directory.
		/// </summary>
		/// <param name="path">The file or directory to move.</param>
		/// <param name="dest">The destination directory to move to.</param>
		/// <param name="newName">The name of the new file or directory.</param>
		/// <remarks>
		/// <para>All cache entries related will be cleared.</para>
		/// <para>This method will throw an exception when the destination path equals to the source path.</para>
		/// </remarks>
		public override async Task MoveItemAsync(string path, string dest, string newName)
		{
			await base.MoveItemAsync(path, dest, newName);
			SplitPath(path, out var dir, out var name);
			var newPath = $"{(dest == "/" ? string.Empty : dest)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", dest + "$", newPath + "$", newPath + "/");
		}

		/// <summary>
		/// Renames a file or a directory.
		/// </summary>
		/// <param name="path">The file or directory to rename.</param>
		/// <param name="newName">The name of the new file or directory.</param>
		/// <remarks>
		/// <para>All cache entries related will be cleared.</para>
		/// <para>This method will throw an exception when the destination path equals to the source path.</para>
		/// </remarks>
		public override async Task RenameItemAsync(string path, string newName)
		{
			await base.RenameItemAsync(path, newName);
			SplitPath(path, out var dir, out var name);
			var newPath = $"{(dir == "/" ? string.Empty : dir)}/{newName}";
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$", newPath + "$", newPath + "/");
		}

		/// <summary>
		/// Creates a new directory.
		/// </summary>
		/// <param name="path">The new directory to create.</param>
		/// <remarks>
		/// <para>All cache entries related will be cleared.</para>
		/// <para>This method will create directories recursively if part of <paramref name="path" /> doesn't exist.</para>
		/// </remarks>
		public override async Task CreateDirectoryAsync(string path)
		{
			await base.CreateDirectoryAsync(path);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "/", path + "$");
		}

		/// <summary>
		/// Uploads a file.
		/// </summary>
		/// <param name="path">The location to store the uploaded file.</param>
		/// <param name="overwrite">Specifies whether to overwrite any existing files.</param>
		/// <param name="data">The data to be uploaded.</param>
		/// <remarks>
		/// <para>This method can upload 2GB data at most.</para>
		/// <para>This method will create directories recursively if part of <paramref name="path" /> doesn't exist.</para>
		/// <para>Whether the uploaded data is buffered depends on whether the length of <paramref name="data" /> is determined.</para>
		/// <para>All cache entries related will be cleared.</para>
		/// </remarks>
		public override async Task UploadFileAsync(string path, bool overwrite, HttpContent data)
		{
			await base.UploadFileAsync(path, overwrite, data);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "$");
		}

		/// <summary>
		/// Concatenates uploaded file slices uploaded by <see cref="BaiduPanContext.UploadFileSliceAsync" /> to a complete file.
		/// </summary>
		/// <param name="path">The location to store the concatenated file.</param>
		/// <param name="overwrite">Specifies whether to overwrite any existing files.</param>
		/// <param name="slices">MD5 hash list of the slices to be concatenated.</param>
		/// <remarks>
		/// <para>This method will create directories recursively if part of <paramref name="path" /> doesn't exist.</para>
		/// <para>
		/// <paramref name="slices" /> is the list of MD5 hash values returned by <see cref="BaiduPanContext.UploadFileSliceAsync" />,
		/// it should contain at least two elements.
		/// </para>
		/// <para>All cache entries related will be cleared.</para>
		/// </remarks>
		public override async Task ConcatFileSlicesAsync(string path, bool overwrite, string[] slices)
		{
			await base.ConcatFileSlicesAsync(path, overwrite, slices);
			SplitPath(path, out var dir, out var name);
			RemoveCacheByPrefixes(dir + "$", path + "$");
		}
	}
}
