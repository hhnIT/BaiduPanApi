using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class ConcatFileSlicesParameter
	{
		[JsonProperty("block_list")]
		public string[] FileList;
	}
}
