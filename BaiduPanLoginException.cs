﻿using System.Collections.Generic;

namespace BaiduPanApi
{
	class BaiduPanLoginException : BaiduPanApiException
	{
		internal override Dictionary<int, string> ErrorMessages { get; } = new Dictionary<int, string>
		{
			{ 1, "您输入的帐号格式不正确" },
			{ 2, "您输入的帐号不存在" },
			{ 3, "验证码不存在或已过期,请重新输入" },
			{ 4, "您输入的帐号或密码有误" },
			{ 6, "您输入的验证码有误" },
			{ 7, "密码错误" },
			{ 16, "您的帐号因安全问题已被限制登录" },
			{ 17, "您的帐号已锁定" },
			{ 21, "没有登录权限" },
			{ 257, "请输入验证码" },
			{ 50023, "1个手机号30日内最多换绑3个账号" },
			{ 50024, "注册过于频繁，请稍候再试" },
			{ 50025, "注册过于频繁，请稍候再试；也可以通过上行短信的方式进行注册" },
			{ 100005, "系统错误,请您稍后再试" },
			{ 100023, "开启Cookie之后才能登录" },
			{ 100027, "百度正在进行系统升级，暂时不能提供服务，敬请谅解" },
			{ 110024, "此帐号暂未激活" },
			{ 120019, "请在弹出的窗口操作,或重新登录" },
			{ 120021, "登录失败,请在弹出的窗口操作,或重新登录" },
			{ 200010, "验证码不存在或已过期" },
			{ 400031, "请在弹出的窗口操作,或重新登录" },
			{ 400414, "您的帐号因为安全问题，暂时被冻结，详情请拨打电话010-59059588" },
			{ 400415, "您的帐号因为安全问题，暂时被冻结，详情请拨打电话010-59059588" },
			{ 401007, "您的手机号关联了其他帐号，请选择登录" },
			{ 500010, "登录过于频繁,请24小时后再试" },
			{ -1, "系统错误,请您稍后再试" },
		};

		public BaiduPanLoginException(int errorCode) : base(errorCode) { }
	}
}
