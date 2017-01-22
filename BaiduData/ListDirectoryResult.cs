using System.Collections.Generic;
using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class ListDirectoryResult : ApiResult
	{
		[JsonProperty("list")]
		public IEnumerable<FileInfo> FileList;
	}
}
