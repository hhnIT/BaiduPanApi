using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class QuotaResult : ApiResult
	{
		[JsonProperty("total")]
		public long? TotalSpace;
		[JsonProperty("used")]
		public long? UsedSpace;
	}
}
