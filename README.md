# BaiduPanApi

[![Build status](https://ci.appveyor.com/api/projects/status/y39a55l6la2cjwel?svg=true)](https://ci.appveyor.com/project/ddosolitary/baidupanapi)
[![NuGet status](https://img.shields.io/nuget/v/BaiduPanApi.svg)](https://www.nuget.org/packages/BaiduPanApi)

A .NET wrapper for BaiduPan API.

# 重要的事情说三遍

### 百度垃圾
### 百度垃圾
### 百度垃圾

# 这是什么

这个是一只对百度网盘提供编程访问的API包装库。

# 功能

- 已实现的功能：基本文件操作，不限速的上传、下载；
- 未实现的功能：文件秒传，回收站操作，离线下载，以及其他可能存在的杂七杂八的功能。

# 原理

通过分析得到百度网盘的API，直接发送HTTP请求并解析百度服务器返回的结果来实现功能。

- 登陆和文件基本操作的API基于对百度网盘网页版行为的分析；
- 上传、下载功能利用百度PCS的接口，并参考了[GangZhuo/BaiduPCS](https://github.com/GangZhuo/BaiduPCS)项目。

# 环境要求

项目基于.NET Framework 4.6.2，目前没有发布多目标包的计划，未来有可能迁移至.NET Standard。

# 使用

- 使用NuGet包：https://www.nuget.org/packages/BaiduPanApi
- 在GitHub Release页面下载编译好的`DLL`库：https://github.com/HackingBaidu/BaiduPanApi/releases
- 从源代码编译：需要Visual Studio 2017提供的C# 7支持

# API

`BaiduPanContext`类封装了所有操作，`CachedBaiduPanContext`从其派生并提供了缓存功能。

`BaiduPanApiException`类封装了百度服务器返回的错误信息。

详细情况请阅读由Sandcastle生成的API文档：https://HackingBaidu.github.io/BaiduPanApi/docs

# 鸣谢

本项目源于[@GangZhuo](https://github.com/GangZhuo)的[BaiduPCS](https://github.com/GangZhuo/BaiduPCS)项目的启发，并在上传、下载功能上参考了其实现。

# 许可证

[![License](https://www.gnu.org/graphics/gplv3-127x51.png)](LICENSE)

本项目使用GPLv3许可证，详细信息参见[LICENSE](LICENSE)。

[![License](https://www.gnu.org/graphics/gfdl-logo-small.png)](LICENSE)

本项目的API文档使用GFDLv1.3，详细信息参见[LICENSE](https://github.com/HackingBaidu/BaiduPanApi/blob/gh-pages/LICENSE)。