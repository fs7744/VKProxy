﻿using CoreDemo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy.Core.Hosting;

var app = Host.CreateDefaultBuilder(args).UseVKProxyCore()
    .ConfigureServices(i =>
    {
        //i.AddSingleton<IListenHandler, TcpListenHandler>();
        //i.AddSingleton<IListenHandler, UdpListenHandler>();
        i.AddSingleton<IListenHandler, HttpListenHandler>();
    })
    .Build();

await app.RunAsync();