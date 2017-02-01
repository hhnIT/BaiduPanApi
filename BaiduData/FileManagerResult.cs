#pragma warning disable 0649

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class FileManagerResult : ApiResult
	{
		[JsonProperty("info")]
		[JsonRequired]
		public IEnumerable<ApiResult> ResultList;
	}
}
