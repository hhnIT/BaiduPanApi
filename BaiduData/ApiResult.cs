using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class ApiResult
	{
		[JsonProperty("errno")]
		[JsonRequired]
		public int ErrorCode;
	}
}
