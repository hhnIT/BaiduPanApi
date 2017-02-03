using System;

namespace BaiduPanApi
{
	/// <summary>
	/// Information about a file or a directory.
	/// </summary>
	public struct BaiduPanFileInformation
	{
		/// <summary>
		/// Name of the file or directory.
		/// </summary>
		public string Name;

		/// <summary>
		/// Indicates whether this item is a directory.
		/// </summary>
		public bool IsDirectory;

		/// <summary>
		/// Indicates whether this item is an empty directory.
		/// </summary>
		/// <remarks>
		/// <para><c>true</c> if this item is an directory.</para>
		/// <para><c>false</c> if this item is an non empty directory.</para>
		/// <para><c>null</c> if this item is a file.</para>
		/// </remarks>
		public bool? IsEmptyDirectory;

		/// <summary>
		/// The time when the file or directory is created.
		/// </summary>
		/// <remarks>
		/// The time is in local timezone.
		/// </remarks>
		public DateTime DateCreated;

		/// <summary>
		/// The last time when the file or directory is modified.
		/// </summary>
		/// /// <remarks>
		/// The time is in local timezone.
		/// </remarks>
		public DateTime DateModified;

		/// <summary>
		/// Size of the file.
		/// </summary>
		/// <remarks>
		/// <c>0</c> if this item is a directory.
		/// </remarks>
		public long Size;
	}
}
