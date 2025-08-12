using HostMonitor.Services;
using HostMonitor.Hubs;

var builder = WebApplication.CreateBuilder(args);

// allow reading simple --key=value commandline args like --hosts=host1,host2
builder.Configuration.AddCommandLine(args);

// add SignalR
builder.Services.AddSignalR();

// register hosted background service
builder.Services.AddHostedService<HostMonitorService>();

// static files from wwwroot
var app = builder.Build();
app.UseDefaultFiles(); // will serve wwwroot/index.html by default
app.UseStaticFiles();

app.MapHub<MonitorHub>("/monitor");

// default root just redirects to index.html (optional)
app.MapGet("/", () => Results.Redirect("/index.html"));

var port = int.TryParse(builder.Configuration["port"], out var p) ? p : 5000;
app.Run($"http://0.0.0.0:{port}");