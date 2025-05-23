# VKProxy
l4/l7 proxy base on Kestrel (c#)

# Doc

Please see  [Doc](https://fs7744.github.io/VKProxy.Doc/docs/introduction.html)

# Features

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

