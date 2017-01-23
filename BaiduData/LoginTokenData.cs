using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	struct LoginTokenData
	{
		[JsonProperty("token")]
		public string LoginToken;
	}
}
