FROM mcr.microsoft.com/dotnet/aspnet:10.0
MAINTAINER Victor.x.qu

RUN curl -sSL -O https://packages.microsoft.com/config/<distribution>/<version>/packages-microsoft-prod.deb && \
	dpkg -i packages-microsoft-prod.deb && \
	rm packages-microsoft-prod.deb && \
	apt-get update
RUN apt-get install libmsquic

ENV ETCD_CONNECTION_STRING=http://127.0.0.1:2379
ENV ETCD_PREFIX=/ReverseProxy/

ADD bin/Release/net10.0 /app
WORKDIR /app
CMD ["dotnet", "VKProxy.Cli.dll", "proxy"]
