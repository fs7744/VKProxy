using System.Net;
using VKProxy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//builder.Services.Configure<ReverseProxyOptions>(o => o.Section = "ReverseProxy2");
//builder.Services.UseReverseProxy().UseSocks5();

builder.Services.AddAcmeChallenge(o =>
{
    o.NewAccount(new string[] { "mailto:test@xxx.com" });
});

builder.WebHost.UseKestrel(k =>
{
    k.ConfigureHttpsDefaults(i => i.UseAcmeChallenge(k.ApplicationServices));

    //k.Listen(IPAddress.Any, 443,
    //                        o =>
    //                            o.UseHttps(h => h.UseAcmeChallenge(k.ApplicationServices)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();