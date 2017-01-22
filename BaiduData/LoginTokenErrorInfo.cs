using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	struct LoginTokenErrorInfo
	{
		[JsonProperty("no")]
		public int ErrorCode;
	}
}
