namespace BaiduPanApi
{
	/// <summary>
	/// Quota information about a BaiduPan.
	/// </summary>
	public struct BaiduPanQuota
	{
		/// <summary>
		/// Total space available in bytes.
		/// </summary>
		public long TotalSpace;

		/// <summary>
		/// Space already used in bytes.
		/// </summary>
		public long UsedSpace;
	}
}
