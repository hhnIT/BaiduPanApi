using System;
using System.Collections.Generic;

namespace BaiduPanApi
{
	/// <summary>
	/// Represents a error returned by the Baidu servers.
	/// </summary>
	public class BaiduPanApiException : Exception
	{
		internal virtual Dictionary<int, string> ErrorMessages { get; } = new Dictionary<int, string>
		{
			{ 0, "成功" },
			{ 1, "服务器错误 " },
			{ 2, "接口请求错误，请稍候重试" },
			{ 3, "一次操作文件不可超过100个" },
			{ 4, "新文件名错误" },
			{ 5, "目标目录非法" },
			{ 6, "备用" },
			{ 7, "NS非法或无权访问" },
			{ 8, "ID非法或无权访问" },
			{ 9, "申请key失败" },
			{ 10, "创建文件的superfile失败" },
			{ 11, "user_id(或user_name)非法或不存在" },
			{ 12, "部分文件已存在于目标文件夹中" },
			{ 13, "此目录无法共享" },
			{ 14, "系统错误" },
			{ 15, "操作失败" },
			{ 102, "无权限操作该目录" },
			{ 103, "提取码错误" },
			{ 104, "验证cookie无效" },
			{ 111, "当前还有未完成的任务，需完成后才能操作" },
			{ 112, "页面已过期，请刷新后重试" },
			{ 132, "删除文件需要验证您的身份" },
			{ 201, "系统错误" },
			{ 202, "系统错误" },
			{ 203, "系统错误" },
			{ 204, "系统错误" },
			{ 205, "系统错误" },
			{ 211, "无权操作或被封禁" },
			{ 301, "其他请求出错" },
			{ 404, "秒传md5不匹配 rapidupload 错误码" },
			{ 406, "秒传创建文件失败 rapidupload 错误码" },
			{ 407, "fileModify接口返回错误，未返回requestid rapidupload 错误码" },
			{ 501, "获取的LIST格式非法" },
			{ 600, "json解析出错" },
			{ 601, "exception抛出异常" },
			{ 617, "getFilelist其他错误" },
			{ 618, "请求curl返回失败" },
			{ 619, "pcs返回错误码" },
			{ 1024, "云冲印购物车文件15日内无法删除" },
			{ 9100, "你的帐号存在违规行为，已被冻结" },
			{ 9200, "你的帐号存在违规行为，已被冻结" },
			{ 9300, "你的帐号存在违规行为，该功能暂被冻结，" },
			{ 9400, "你的帐号异常，需验证后才能使用该功能" },
			{ 9500, "你的帐号存在安全风险，已进入保护模式，请修改密码后使用" },
			{ 31021, "网络连接失败，请检查网络或稍候再试" },
			{ 31075, "一次支持操作999个，减点试试吧" },
			{ 31080, "我们的服务器出错了，稍候试试吧" },
			{ 31116, "你的空间不足了哟，赶紧购买空间吧" },
			{ -1, "用户名和密码验证失败" },
			{ -2, "备用" },
			{ -3, "用户未激活（调用init接口）" },
			{ -4, "COOKIE中未找到host_key&user_key（或BDUSS）" },
			{ -5, "host_key和user_key无效" },
			{ -6, "登录失败，请重新登录" },
			{ -7, "文件或目录名错误或无权访问" },
			{ -8, "该目录下已存在此文件" },
			{ -9, "文件被所有者删除，操作失败" },
			{ -10, "你的空间不足了哟" },
			{ -11, "父目录不存在" },
			{ -12, "设备尚未注册" },
			{ -13, "设备已经被绑定" },
			{ -14, "帐号已经初始化" },
			{ -21, "预置文件无法进行相关操作" },
			{ -22, "被分享的文件无法重命名，移动等操作" },
			{ -23, "数据库操作失败，请联系netdisk管理员" },
			{ -24, "要取消的文件列表中含有不允许取消public的文件。" },
			{ -25, "非公测用户" },
			{ -26, "邀请码失效" },
			{ -102, "云冲印文件7日内无法删除" },
			{ -32, "你的空间不足了哟" }
		};

		/// <summary>
		/// Gets the error code of the error.
		/// </summary>
		/// <value>The error code of the error.</value>
		public int ErrorCode { get; }

		/// <summary>
		/// Gets whether this error is unknown.
		/// </summary>
		/// <value><c>true</c> if this error is unknown.</value>
		/// <remarks>
		/// An error is considered unknown if Baidu doesn't provide an error message for its error code.
		/// </remarks>
		public bool IsUnknownError => !ErrorMessages.ContainsKey(ErrorCode);

		/// <summary>
		/// Gets the error message corresponds to <see cref="ErrorCode" />.
		/// </summary>
		/// <value>The error message corresponds to <see cref="ErrorCode" />.</value>
		public override string Message => ErrorMessages.TryGetValue(ErrorCode, out var msg) ? msg : "Unknown error";

		/// <summary>
		/// Creates an instance of <see cref="BaiduPanApiException" />.
		/// </summary>
		/// <param name="errorCode">The error code of the error.</param>
		public BaiduPanApiException(int errorCode) { ErrorCode = errorCode; }
	}
}
