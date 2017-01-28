﻿#pragma warning disable 0649

using Newtonsoft.Json;

namespace BaiduPanApi.BaiduData
{
	[JsonObject(ItemRequired = Required.Always)]
	class LoginTokenErrorInfo
	{
		[JsonProperty("no")]
		public int ErrorCode;
	}
}
