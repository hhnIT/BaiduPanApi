using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BaiduPanApi
{
	public class BaiduPanContext : IDisposable
	{
		private const string
			ClientUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.67 Safari/537.36",
			PcsAppId = "250528",
			UploadContentType = "application/octet-stream",
			UploadContentDisposition = "form-data",
			UploadFileSliceMd5Header = "Content-MD5",

			BaiduHomeUrl = "https://www.baidu.com/",
			BaiduPassportUrl = "https://passport.baidu.com/",
			BaiduLoginApiUrl = BaiduPassportUrl + "v2/api/",
			BaiduPanHomeUrl = "https://pan.baidu.com/",
			BaiduPanApiUrl = BaiduPanHomeUrl + "api/",
			BaiduPanPcsUrl = "https://pcs.baidu.com/rest/2.0/pcs/",

			GetLoginTokenUrl = "?getapi&tpl=netdisk&apiver=v3",
			CaptchaUrl = "cgi-bin/genimage?{0}",
			LoginUrl = "?login",
			LogoutUrl = "?logout",
			QuotaUrl = "quota",
			ListDirectoryUrl = "list?web=1&dir={0}",
			SearchUrl = "search?dir={0}&key={1}",
			SearchRecursionUrl = SearchUrl + "&recursion",
			DeleteUrl = "filemanager?opera=delete&bdstoken={0}",
			MoveUrl = "filemanager?opera=move&bdstoken={0}",
			RenameUrl = "filemanager?opera=rename&bdstoken={0}",
			CreateDirectoryUrl = "create?bdstoken={0}",
			DownloadFileUrl = "file?method=download&app_id={0}&path={1}",
			UploadFileUrl = "file?method=upload&app_id={0}&path={1}",
			UploadFileOverwriteUrl = "file?method=upload&ondup=overwrite&app_id={0}&path={1}",
			UploadFileSliceUrl = "file?method=upload&type=tmpfile&app_id={0}",
			ConcatFileSlicesUrl = "file?method=createsuperfile&app_id={0}&path={1}",
			ConcatFileSlicesOverwriteUrl = ConcatFileSlicesUrl + "&ondup=overwrite",

			Hex128BitRegex = "[a-z0-9]{32}",
			BdsTokenRegex = "\"bdstoken\":\"(" + Hex128BitRegex + ")\"",
			LoginErrorCodeRegex = "err_no=([0-9]+)",
			CodeStringRegex = "codeString=([a-zA-Z0-9]+)",

			CookieNotFoundErrorMessage = "Cound not find the cookie named \"{0}\".",
			FormatErrorMessage = "Could not parse the response returned by the BaiduPan server.";

		public string Username { get; }

		private readonly HttpClient client;
		private readonly HttpClientHandler handler;
		private readonly string bdsToken;
		private bool disposed;

		public BaiduPanContext(string username, string password, Func<byte[], string> captchaCallback)
		{
			handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
			GetBaiduIdAsync().Wait();
			LoginAsync(username, password, GetLoginTokenAsync().Result, captchaCallback).Wait();
			bdsToken = GetBdsTokenAsync().Result;
			Username = username;
			client = new HttpClient(handler) { BaseAddress = new Uri(BaiduPanApiUrl) };
		}

		protected virtual async Task DisposeAsync()
		{
			await LogoutAsync();
			client.Dispose();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				DisposeAsync().Wait();
			}
		}

		~BaiduPanContext() { Dispose(); }

		#region Helpers

		private static void CheckResponseStatus(HttpResponseMessage response, bool allowPartialContent = false)
		{
			if (response.StatusCode != HttpStatusCode.OK && (allowPartialContent ? response.StatusCode != HttpStatusCode.PartialContent : true))
				throw new HttpRequestException(response.ReasonPhrase);
		}

		private static async Task<T> ParseJsonAsync<T>(HttpResponseMessage response)
		{
			try { return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync()); }
			catch (Exception ex) { throw new FormatException(FormatErrorMessage, ex); }
		}

		private static Uri GetUri(string template, params string[] values)
			=> new Uri(string.Format(template, values.Select(WebUtility.UrlEncode).ToArray()), UriKind.Relative);

		protected static void SplitPath(string path, out string dir, out string name)
		{
			var index = path.LastIndexOf('/');
			dir = index == 0 ? "/" : path.Substring(0, index);
			name = path.Substring(index + 1);
		}

		#endregion

		#region Account

		private async Task GetBaiduIdAsync()
		{
			using (var client = new HttpClient(handler, false))
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd(ClientUserAgent);
				using (var response = await client.GetAsync(BaiduHomeUrl))
					CheckResponseStatus(response);
			}
		}

		private async Task<string> GetLoginTokenAsync()
		{
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduLoginApiUrl) })
			using (var response = await client.GetAsync(GetUri(GetLoginTokenUrl)))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.LoginTokenResult>(response);
				if (result.ErrorInfo.ErrorCode != 0) throw new BaiduPanLoginException(result.ErrorInfo.ErrorCode);
				var token = result.Data.LoginToken;
				if (!Regex.IsMatch(token, $"^{Hex128BitRegex}$")) throw new FormatException(FormatErrorMessage);
				return token;
			}
		}

		private async Task<byte[]> GetCaptchaAsync(string codeString)
		{
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPassportUrl) })
			using (var response = await client.GetAsync(GetUri(CaptchaUrl, codeString)))
			{
				CheckResponseStatus(response);
				return await response.Content.ReadAsByteArrayAsync();
			}
		}

		private async Task<string> DoLoginRequestAsync(string username, string password, string token, string captcha)
		{
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduLoginApiUrl) })
			{
				var paramDict = new Dictionary<string, string>
				{
					{ "tpl", "netdisk" },
					{ "apiver", "v3" },
					{ "username", username },
					{ "password", password },
					{ "token", token }
				};
				if (captcha != null) paramDict.Add("verifycode", captcha);
				using (var content = new FormUrlEncodedContent(paramDict))
				using (var response = await client.PostAsync(GetUri(LoginUrl), content))
				{
					CheckResponseStatus(response);
					return await response.Content.ReadAsStringAsync();
				}
			}
		}

		private static int GetLoginErrorCodeAsync(string result)
		{
			var errorCodeMatch = Regex.Match(result, LoginErrorCodeRegex);
			if (!errorCodeMatch.Success) throw new FormatException(FormatErrorMessage);
			return int.Parse(errorCodeMatch.Groups[1].Value);
		}

		private async Task LoginAsync(string username, string password, string token, Func<byte[], string> captchaCallback)
		{
			var result = await DoLoginRequestAsync(username, password, token, null);
			var errorCode = GetLoginErrorCodeAsync(result);
			if (errorCode == 257 && captchaCallback != null)
			{
				var codeStringMatch = Regex.Match(result, CodeStringRegex);
				if (!codeStringMatch.Success) throw new FormatException(FormatErrorMessage);
				result = await DoLoginRequestAsync(username, password, token, captchaCallback(await GetCaptchaAsync(codeStringMatch.Value)));
				errorCode = GetLoginErrorCodeAsync(result);
			}
			if (errorCode != 0 && errorCode != 18) throw new BaiduPanLoginException(errorCode);
		}

		private async Task<string> GetBdsTokenAsync()
		{
			using (var client = new HttpClient(handler, false))
			{
				var response = await client.GetAsync(new Uri(BaiduPanHomeUrl));
				CheckResponseStatus(response);
				var match = Regex.Match(await response.Content.ReadAsStringAsync(), BdsTokenRegex);
				if (!match.Success) throw new FormatException(FormatErrorMessage);
				return match.Groups[1].Value;
			}
		}

		private async Task LogoutAsync()
		{
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPassportUrl) })
				CheckResponseStatus(await client.GetAsync(GetUri(LogoutUrl)));
		}

		#endregion

		#region Operations

		public virtual async Task<BaiduPanQuota> GetQuotaAsync()
		{
			using (var response = await client.GetAsync(GetUri("quota")))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.QuotaResult>(response);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
				if (result.TotalSpace == null || result.UsedSpace == null) throw new FormatException(FormatErrorMessage);
				return new BaiduPanQuota
				{
					TotalSpace = result.TotalSpace.Value,
					UsedSpace = result.UsedSpace.Value
				};
			}
		}

		public virtual async Task<IEnumerable<BaiduPanFileInformation>> ListDirectoryAsync(string path)
		{
			using (var response = await client.GetAsync(GetUri(ListDirectoryUrl, path)))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.ListDirectoryResult>(response);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
				if (result.FileList == null) throw new FormatException(FormatErrorMessage);
				return result.FileList.Select(file => new BaiduPanFileInformation
				{
					Name = file.Name,
					DateCreated = DateTimeOffset.FromUnixTimeSeconds(file.DateCreated).LocalDateTime,
					DateModified = DateTimeOffset.FromUnixTimeSeconds(file.DateModified).LocalDateTime,
					IsDirectory = file.IsDirectory != 0,
					IsEmptyDirectory = file.IsEmptyDirectory.HasValue ? (bool?)(file.IsEmptyDirectory != 0) : null,
					Size = file.Size
				});
			}
		}

		public virtual async Task<BaiduPanFileInformation> GetItemInformationAsync(string path)
		{
			SplitPath(path, out var dir, out var name);
			try { return (await ListDirectoryAsync(dir)).First(file => string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)); }
			catch (InvalidOperationException ex) { throw new FileNotFoundException(string.Empty, ex); }
		}

		public virtual async Task<IEnumerable<BaiduPanFileInformation>> SearchAsync(string path, string key, bool recursive)
		{
			using (var response = await client.GetAsync(GetUri(recursive ? SearchRecursionUrl : SearchUrl, path, key)))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.ListDirectoryResult>(response);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
				if (result.FileList == null) throw new FormatException(FormatErrorMessage);
				return result.FileList.Select(file => new BaiduPanFileInformation
				{
					Name = file.Name,
					DateCreated = DateTimeOffset.FromUnixTimeSeconds(file.DateCreated).LocalDateTime,
					DateModified = DateTimeOffset.FromUnixTimeSeconds(file.DateModified).LocalDateTime,
					IsDirectory = file.IsDirectory != 0,
					Size = file.Size
				});
			}
		}

		public virtual async Task DeleteItemAsync(string path)
		{
			using (var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("filelist", JsonConvert.SerializeObject(new[] { path })) }))
			using (var response = await client.PostAsync(GetUri(DeleteUrl, bdsToken), content))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.FileManagerResult>(response);
				var errorCode = result.ResultList.First().ErrorCode;
				if (errorCode != 0) throw new BaiduPanApiException(errorCode);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
			}
		}

		public virtual async Task MoveItemAsync(string path, string dest, string newName)
		{
			using (var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("filelist", JsonConvert.SerializeObject(new[]
				{
					new {
						path = path,
						dest = dest,
						newname = newName
					}
				}))
			}))
			using (var response = await client.PostAsync(GetUri(MoveUrl, bdsToken), content))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.FileManagerResult>(response);
				var errorCode = result.ResultList.First().ErrorCode;
				if (errorCode != 0) throw new BaiduPanApiException(errorCode);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
			}
		}

		public virtual async Task RenameItemAsync(string path, string newName)
		{
			using (var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("filelist", JsonConvert.SerializeObject(new[]
				{
					new {
						path = path,
						newname = newName
					}
				}))
			}))
			using (var response = await client.PostAsync(GetUri(RenameUrl, bdsToken), content))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.FileManagerResult>(response);
				var errorCode = result.ResultList.First().ErrorCode;
				if (errorCode != 0) throw new BaiduPanApiException(errorCode);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
			}
		}

		public virtual async Task CreateDirectoryAsync(string path)
		{
			var paramDict = new Dictionary<string, string>
			{
				{ "isdir", "1" },
				{ "path", path }
			};
			using (var content = new FormUrlEncodedContent(paramDict))
			using (var response = await client.PostAsync(GetUri(CreateDirectoryUrl, bdsToken), content))
			{
				CheckResponseStatus(response);
				var result = await ParseJsonAsync<BaiduData.ApiResult>(response);
				if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
			}
		}

		public virtual async Task DownloadFileAsync(string path, RangeHeaderValue range, Action<Stream> processDownload)
		{
			if (processDownload == null) throw new ArgumentNullException(nameof(processDownload));
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPanPcsUrl) })
			using (var request = new HttpRequestMessage { RequestUri = GetUri(DownloadFileUrl, PcsAppId, path) })
			{
				request.Headers.Range = range;
				using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
				{
					CheckResponseStatus(response, true);
					processDownload(await response.Content.ReadAsStreamAsync());
				}
			}
		}

		public virtual async Task UploadFileAsync(string path, bool overwrite, HttpContent data)
		{
			SplitPath(path, out var dir, out var name);
			data.Headers.ContentType = new MediaTypeHeaderValue(UploadContentType);
			data.Headers.ContentDisposition = new ContentDispositionHeaderValue(UploadContentDisposition)
			{
				Name = "file",
				FileName = name
			};
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPanPcsUrl) })
			using (var content = new MultipartFormDataContent { data })
			using (var response = await client.PostAsync(GetUri(overwrite ? UploadFileOverwriteUrl : UploadFileUrl, PcsAppId, path), content))
				CheckResponseStatus(response);
		}

		public virtual async Task<string> UploadFileSliceAsync(HttpContent data)
		{
			data.Headers.ContentType = new MediaTypeHeaderValue(UploadContentType);
			data.Headers.ContentDisposition = new ContentDispositionHeaderValue(UploadContentDisposition)
			{
				Name = "file",
				FileName = "foo"
			};
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPanPcsUrl) })
			using (var content = new MultipartFormDataContent { data })
			using (var response = await client.PostAsync(GetUri(UploadFileSliceUrl, PcsAppId), content))
			{
				CheckResponseStatus(response);
				var md5 = response.Content.Headers.GetValues(UploadFileSliceMd5Header).First();
				if (!Regex.IsMatch(md5, $"^{Hex128BitRegex}$")) throw new FormatException(FormatErrorMessage);
				return md5;
			}
		}

		public virtual async Task ConcatFileSlicesAsync(string path, bool overwrite, string[] slices)
		{
			using (var client = new HttpClient(handler, false) { BaseAddress = new Uri(BaiduPanPcsUrl) })
			using (var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>(
					"param",
					JsonConvert.SerializeObject(new BaiduData.ConcatFileSlicesParameter { FileList = slices })
				)
			}))
			using (var response = await client.PostAsync(GetUri(overwrite ? ConcatFileSlicesOverwriteUrl : ConcatFileSlicesUrl, PcsAppId, path), content))
				CheckResponseStatus(response);
		}

		#endregion
	}
}
