using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	class FileInfo
	{
		[JsonProperty("server_filename")]
		public string Name;
		[JsonProperty("server_ctime")]
		public long DateCreated;
		[JsonProperty("server_mtime")]
		public long DateModified;
		[JsonProperty("isdir")]
		public int IsDirectory;
		[JsonProperty("dir_empty", Required = Required.Default)]
		public int? IsEmptyDirectory;
		[JsonProperty("size")]
		public long Size;
	}
}
