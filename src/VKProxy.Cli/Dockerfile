FROM mcr.microsoft.com/dotnet/aspnet:9.0
MAINTAINER Victor.x.qu

ENV ETCD_CONNECTION_STRING=http://127.0.0.1:2379
ENV ETCD_PREFIX=/ReverseProxy/

ADD bin/Release/net9.0 /app
WORKDIR /app
CMD ["dotnet", "VKProxy.Cli.dll", "proxy"]
