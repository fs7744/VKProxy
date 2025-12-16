FROM mcr.microsoft.com/dotnet/aspnet:10.0
MAINTAINER Victor.x.qu

RUN sudo add-apt-repository "deb [arch=amd64] https://packages.microsoft.com/ubuntu/22.04/prod jammy main"
RUN sudo apt update
RUN sudo apt install libmsquic

ENV ETCD_CONNECTION_STRING=http://127.0.0.1:2379
ENV ETCD_PREFIX=/ReverseProxy/

ADD bin/Release/net10.0 /app
WORKDIR /app
CMD ["dotnet", "VKProxy.Cli.dll", "proxy"]
