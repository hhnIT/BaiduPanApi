#pragma warning disable 0649

using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class UploadSliceResult
	{
		[JsonProperty("md5")]
		[JsonRequired]
		public string Md5;
	}
}
