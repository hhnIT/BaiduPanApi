using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	class LoginTokenData
	{
		[JsonProperty("token")]
		public string LoginToken;
	}
}
