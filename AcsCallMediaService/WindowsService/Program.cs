using Microsoft.AspNetCore.Server.Kestrel.Core;
using WindowsService.Services;

if (!OperatingSystem.IsWindows())
	throw new Exception("This service can only run on Windows!");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	// for better perf use named pipes instead of going over Tcp
	serverOptions.ListenNamedPipe("GrpcPipe", listenOptions =>
	{
		listenOptions.Protocols = HttpProtocols.Http2;
	});
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CommandService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
