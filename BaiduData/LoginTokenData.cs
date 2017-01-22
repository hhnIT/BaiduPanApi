using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	public struct LoginTokenData
	{
		[JsonProperty("token")]
		public string LoginToken;
	}
}
