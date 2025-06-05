# VKProxy
l4/l7 proxy base on Kestrel (c#)

[VKProxy](https://github.com/fs7744/VKProxy) 是使用c#开发的基于 [Kestrel](https://github.com/dotnet/aspnetcore/tree/main/src/Servers/Kestrel) 实现 L4/L7的代理

## 为何基于 Kestrel

主要是http 协议的处理实在太多了，工作量巨大，所以为了省事就基于 Kestrel 了

不过众所周知 Kestrel 是 Aspnetcore 为了跨平台而实现的web server，只提供 http 1/2/3 的 L7层的能力

但看过源码的同学都知道，其实其本身从L4层(socket)实现的Http协议处理，只是[OnBind](https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Core/src/Internal/KestrelServerImpl.cs#L137)只有http相关实现以及没有提供相关公开扩展的api，所以限制了其能力

但是既然代码是开源的，并且我们也知道dotnet有虽然麻烦但是能跨越访问限制的能力（Reflection），所以它是不能阻挡我们的魔爪

（ps 
     1. 不过这样绕过限制可能会在[Native AOT](https://learn.microsoft.com/zh-cn/dotnet/core/deploying/native-aot/?tabs=windows%2Cnet8)相关场景存在问题，目前暂时没有做具体相关测试
     2. 在不同版本Kestrel 可能会存在api变动，目前为了省事，不适配各版本差异，暂时以net9.0为准，net10正式发布后迁移升级到net10，此后不再适配net9.0之前版本
 ）

### 局限

不得不先提一个局限，dotnet 的socket 没有提供统一的跨进程socket转移api，因为dotnet是跨平台的，不同系统存在差异，该issue [Migrate Socket between processes](https://github.com/dotnet/runtime/issues/48637) 已经多年没有下文了

所以不好做到热重启，暂时不会支持

不过内部有支持监听配置变动，进行相关监听端口变动处理等，所以大部分场景应该没有太大问题，只是无法保持tcp连接迁移


## 可扩展套接字应用程序框架

除了代理功能外，由于通过反射释放了Kestrel的能力，你也可以把 VKProxy 当成可扩展套接字应用程序框架使用

使用它轻松构建始终连接的套接字应用程序，而无需考虑如何使用套接字，如何维护套接字连接以及套接字如何工作。

(在Kestrel基础上开发，理论可以帮大家节省一些比如直接使用socket要自己管理 socket之类的事情)

具体可以参考[可扩展套接字应用程序框架](https://fs7744.github.io/VKProxy.Doc/docs/extensibility/socket.html)

## Doc

Please see  [Doc](https://fs7744.github.io/VKProxy.Doc/docs/introduction.html)

## Features

- [X] TCP proxy
- [X] UDP proxy
- [X] HTTP1/2/3 proxy
- [X] SNI proxy (no tls handle, tls base on upstream)
- [X] SNI proxy (tls handle, upstream no tls handle)
- [X] dns (use system dns, no query from dns server )
- [X] LoadBalancingPolicy
- [X] Passive HealthCheck
- [X] TCP Connected Active HealthCheck
- [X] Configuration 
- [X] reload config and rebind
- [X] socks5 TCP
	- [X] NO AUTH
	- [X] simple user/password auth
- [X] socks5 UDP
- [X] Http Active HealthCheck
- [X] socks5(tcp) to websocket to socks5
- [X] etcd storage
- [ ] simple config web ui

