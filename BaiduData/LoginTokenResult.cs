using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	struct LoginTokenResult
	{
		[JsonProperty("data")]
		public LoginTokenData Data;
		[JsonProperty("errInfo")]
		public LoginTokenErrorInfo ErrorInfo;
	}
}
