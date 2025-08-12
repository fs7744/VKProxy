using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "https://localhost:4001;http://localhost:4000");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource.AddService("TestApi", "").AddContainerDetector())
                    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
                    .WithMetrics(builder =>
                    {
                        builder.AddMeter("System.Runtime", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.MemoryPool");
                    })
                    .WithLogging()
                    .UseOtlpExporter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();