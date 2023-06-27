using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using WindowsService;
using WindowsService.Services;

if (!OperatingSystem.IsWindows())
	throw new Exception("This service can only run on Windows!");

var builder = WebApplication.CreateBuilder(args);

// not sure how to use named pipes and also have TCP / HTTP endpoints
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//	// for better perf use named pipes instead of going over Tcp
//	serverOptions.ListenNamedPipe("GrpcPipe", listenOptions =>
//	{
//		listenOptions.Protocols = HttpProtocols.Http2;
//	});
//});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<AcsWindowsClientManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CommandService>();
app.MapGet("/health", () => "Healthy");
app.MapGet("/start", async () =>
{
	await app.Services.GetService<AcsWindowsClientManager>().LaunchApp();

});

app.Run();
