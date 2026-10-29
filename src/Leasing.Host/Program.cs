using Lab.Core.MessageHubs;
using Leasing.AppServices;
using Leasing.Host.Authorization;
using Leasing.Host.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLeasingAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddLeasingAppServices(builder.Configuration);

var signalR = builder.Services.AddSignalR();
var azureSignalR = builder.Configuration["Azure:SignalR:ConnectionString"];
if (!string.IsNullOrEmpty(azureSignalR))
{
    signalR.AddAzureSignalR(azureSignalR);
}

builder.Services.AddMessageHubUtility<LeasingHub>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LeasingHub>(LeasingHub.Route);
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
