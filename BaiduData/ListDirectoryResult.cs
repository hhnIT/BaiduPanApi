using System.Collections.Generic;
#pragma warning disable 0649

using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	class ListDirectoryResult : ApiResult
	{
		[JsonProperty("list")]
		public IEnumerable<FileInfo> FileList;
	}
}
