using System;

namespace BaiduPanApi
{
	public struct BaiduPanFileInformation
	{
		public string Name;
		public bool IsDirectory;
		public bool? IsEmptyDirectory;
		public DateTime DateCreated, DateModified;
		public long Size;
	}
}
