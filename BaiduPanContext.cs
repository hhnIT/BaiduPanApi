using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions;
using System.Web;

namespace BaiduPanApi
{
	public class BaiduPanContext : IDisposable
	{
		private const string
			ClientUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.67 Safari/537.36",
			PcsAppId = "250528",
			BaiduHomeUrl = "https://www.baidu.com",
			BaiduPassportUrl = "https://passport.baidu.com",
			BaiduLoginApiUrl = BaiduPassportUrl + "/v2/api",
			BaiduCaptchaUrl = BaiduPassportUrl + "/cgi-bin/genimage",
			BaiduPanHomeUrl = "https://pan.baidu.com",
			BaiduPanApiUrl = BaiduPanHomeUrl + "/api",
			BaiduPanPcsUrl = "https://pcs.baidu.com/rest/2.0/pcs",
			TokenRegex = "[a-z0-9]{32}",
			LoginTokenRegex = "^" + TokenRegex + "$",
			BdsTokenRegex = "\"bdstoken\":\"(" + TokenRegex + ")\"",
			LoginErrorCodeRegex = "err_no=([0-9]+)",
			CodeStringRegex = "codeString=([a-zA-Z0-9]+)",
			CookieNotFoundErrorMessage = "Cound not find the cookie named \"{0}\".",
			FormatErrorMessage = "Could not parse the response returned by the BaiduPan server.";

		public string Username { get; }

		private readonly RestClient client;
		private readonly string bdsToken;

		public BaiduPanContext(string username, string password, Func<byte[], string> captchaCallback)
		{
			var cookies = new CookieContainer();

			GetBaiduId(cookies);
			Login(username, password, GetLoginToken(cookies), captchaCallback, cookies);

			bdsToken = GetBdsToken(cookies);
			client = new RestClient(BaiduPanApiUrl) { CookieContainer = cookies };
			Username = username;
		}

		#region IDisposable

		public void Dispose() { Logout(client.CookieContainer); }

		~BaiduPanContext() { Dispose(); }

		#endregion

		#region Helpers

		private void CheckResponseStatus(IRestResponse response)
		{
			if (response.ResponseStatus != ResponseStatus.Completed)
				throw response.ErrorException ?? response.ResponseStatus.ToWebException();
			if (response.StatusCode != HttpStatusCode.OK)
				throw new WebException(response.StatusDescription);
		}

		private void CheckCookie(IRestResponse response, string cookieName)
		{
			if (!response.Cookies.Any(cookie => cookie.Name == cookieName))
				throw new WebException(string.Format(CookieNotFoundErrorMessage, cookieName));
		}

		private T ParseJson<T>(IRestResponse response)
		{
			try { return JsonConvert.DeserializeObject<T>(response.Content); }
			catch (Exception ex) { throw new FormatException(FormatErrorMessage, ex); }
		}

		protected void SplitPath(string path, out string dir, out string name)
		{
			var index = path.LastIndexOf('/');
			dir = index == 0 ? "/" : path.Substring(0, index);
			name = path.Substring(index + 1);
		}

		#endregion

		#region Account

		private void GetBaiduId(CookieContainer cookies)
		{
			var client = new RestClient(BaiduHomeUrl) { CookieContainer = cookies, UserAgent = ClientUserAgent };
			var response = client.Execute(new RestRequest());
			CheckResponseStatus(response);
			CheckCookie(response, "BAIDUID");
		}

		private string GetLoginToken(CookieContainer cookies)
		{
			var client = new RestClient(BaiduLoginApiUrl) { CookieContainer = cookies };
			var request = new RestRequest("?getapi");
			request.AddParameter("tpl", "netdisk");
			request.AddParameter("apiver", "v3");
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.LoginTokenResult>(response);
			if (result.ErrorInfo.ErrorCode != 0) throw new BaiduPanLoginException(result.ErrorInfo.ErrorCode);
			var token = result.Data.LoginToken;
			if (!Regex.IsMatch(token, LoginTokenRegex)) throw new FormatException(FormatErrorMessage);
			return token;
		}

		private byte[] GetCaptcha(string codeString, CookieContainer cookies)
		{
			var client = new RestClient(BaiduCaptchaUrl) { CookieContainer = cookies };
			var response = client.Execute(new RestRequest($"?{codeString}"));
			CheckResponseStatus(response);
			return response.RawBytes;
		}

		private IRestResponse DoLoginRequest(string username, string password, string token, string captcha, CookieContainer cookies)
		{
			var client = new RestClient(BaiduLoginApiUrl) { CookieContainer = cookies };
			var request = new RestRequest("?login", Method.POST);
			request.AddParameter("tpl", "netdisk");
			request.AddParameter("apiver", "v3");
			request.AddParameter("username", username);
			request.AddParameter("password", password);
			request.AddParameter("token", token);
			if (captcha != null) request.AddParameter("verifycode", captcha);
			var response = client.Execute(request);
			CheckResponseStatus(response);
			return response;
		}

		private int GetLoginErrorCode(IRestResponse response)
		{
			var errorCodeMatch = Regex.Match(response.Content, LoginErrorCodeRegex);
			if (!errorCodeMatch.Success) throw new FormatException(FormatErrorMessage);
			return int.Parse(errorCodeMatch.Groups[1].Value);
		}

		private void Login(string username, string password, string token, Func<byte[], string> captchaCallback, CookieContainer cookies)
		{
			var response = DoLoginRequest(username, password, token, null, cookies);
			var errorCode = GetLoginErrorCode(response);
			if (errorCode == 257 && captchaCallback != null)
			{
				var codeStringMatch = Regex.Match(response.Content, CodeStringRegex);
				if (!codeStringMatch.Success) throw new FormatException(FormatErrorMessage);
				response = DoLoginRequest(username, password, token, captchaCallback(GetCaptcha(codeStringMatch.Value, cookies)), cookies);
				errorCode = GetLoginErrorCode(response);
			}
			if (errorCode != 0 && errorCode != 18) throw new BaiduPanLoginException(errorCode);
			CheckCookie(response, "BDUSS");
		}

		private string GetBdsToken(CookieContainer cookies)
		{
			var client = new RestClient(BaiduPanHomeUrl) { CookieContainer = cookies };
			var response = client.Execute(new RestRequest());
			CheckResponseStatus(response);
			var match = Regex.Match(response.Content, BdsTokenRegex);
			if (!match.Success) throw new FormatException(FormatErrorMessage);
			return match.Groups[1].Value;
		}

		private void Logout(CookieContainer cookies)
		{
			var client = new RestClient(BaiduPassportUrl) { CookieContainer = cookies };
			CheckResponseStatus(client.Execute(new RestRequest("?logout")));
		}

		#endregion

		#region Operations

		public virtual IEnumerable<BaiduPanFileInformation> ListDirectory(string path)
		{
			var request = new RestRequest("list");
			request.AddParameter("dir", path);
			request.AddParameter("web", 1);
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ListDirectoryResult>(response);
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

		public virtual BaiduPanFileInformation GetItemInformation(string path)
		{
			SplitPath(path, out var dir, out var name);
			try { return ListDirectory(dir).First(file => string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)); }
			catch (InvalidOperationException ex) { throw new FileNotFoundException(string.Empty, ex); }
		}

		public virtual IEnumerable<BaiduPanFileInformation> Search(string path, string key, bool recursive)
		{
			var request = new RestRequest("search");
			request.AddParameter("dir", path);
			request.AddParameter("key", key);
			if (recursive) request.AddParameter("recursion", 1);
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ListDirectoryResult>(response);
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

		public virtual void DeleteItem(string path)
		{
			var request = new RestRequest("filemanager", Method.POST);
			request.AddQueryParameter("opera", "delete");
			request.AddQueryParameter("bdstoken", bdsToken);
			request.AddParameter("filelist", JsonConvert.SerializeObject(new[] { path }));
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ApiResult>(response);
			if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
		}

		public virtual void MoveItem(string path, string dest, string newName)
		{
			var request = new RestRequest("filemanager", Method.POST);
			request.AddQueryParameter("opera", "move");
			request.AddQueryParameter("bdstoken", bdsToken);
			request.AddParameter("filelist", JsonConvert.SerializeObject(new[]
			{
				new
				{
					path = path,
					dest = dest,
					newname = newName
				}
			}));
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ApiResult>(response);
			if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
		}

		public virtual void RenameItem(string path, string newName)
		{
			var request = new RestRequest("filemanager", Method.POST);
			request.AddQueryParameter("opera", "rename");
			request.AddQueryParameter("bdstoken", bdsToken);
			request.AddParameter("filelist", JsonConvert.SerializeObject(new[]
			{
				new
				{
					path = path,
					newname = newName
				}
			}));
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ApiResult>(response);
			if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
		}

		public virtual void CreateDirectory(string path)
		{
			var request = new RestRequest("create", Method.POST);
			request.AddQueryParameter("bdstoken", bdsToken);
			request.AddParameter("path", path);
			request.AddParameter("isdir", 1);
			var response = client.Execute(request);
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.ApiResult>(response);
			if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
		}

		public virtual IRestResponse DownloadFile(string path, Action<IRestRequest> beforeDownload, Action<Stream> processDownload)
		{
			var client = new RestClient(BaiduPanPcsUrl) { CookieContainer = this.client.CookieContainer };
			var request = new RestRequest("file");
			request.AddParameter("method", "download");
			request.AddParameter("app_id", PcsAppId);
			request.AddParameter("path", path);
			request.ResponseWriter = processDownload;
			beforeDownload?.Invoke(request);
			return client.Execute(request);
		}

		public virtual IRestResponse UploadFile(string path, bool overwrite, long fileSize, Action<IRestRequest> beforeUpload, Action<Stream> processUpload)
		{
			var client = new RestClient(BaiduPanPcsUrl) { UserAgent = ClientUserAgent, CookieContainer = this.client.CookieContainer };
			var request = new RestRequest("file", Method.POST);
			request.AddQueryParameter("method", "upload");
			request.AddQueryParameter("app_id", PcsAppId);
			if (overwrite) request.AddQueryParameter("ondup", "overwrite");
			request.AddQueryParameter("path", path);
			SplitPath(path, out var dir, out var name);
			request.Files.Add(new FileParameter() { ContentLength = fileSize, FileName = name, Name = "file", Writer = processUpload });
			beforeUpload?.Invoke(request);
			return client.Execute(request);
		}

		public virtual BaiduPanQuota GetQuota()
		{
			var response = client.Execute(new RestRequest("quota"));
			CheckResponseStatus(response);
			var result = ParseJson<BaiduData.QuotaResult>(response);
			if (result.ErrorCode != 0) throw new BaiduPanApiException(result.ErrorCode);
			if (result.TotalSpace == null || result.UsedSpace == null) throw new FormatException(FormatErrorMessage);
			return new BaiduPanQuota
			{
				TotalSpace = result.TotalSpace.Value,
				UsedSpace = result.UsedSpace.Value
			};
		}

		#endregion
	}
}
