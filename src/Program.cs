using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Prometheus;
using Quartz;
using Quartz.AspNetCore;
using ResLogger2.Common.ServerDatabase;
using ResLogger2.Web.Jobs;
using ResLogger2.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.WebHost.UseKestrel(options =>
{
	options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
});

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddRazorPages();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContextPool<ServerHashDatabase>(
	opt => opt
		.UseNpgsql(Environment.GetEnvironmentVariable("RL2_CONNSTRING")));
builder.Services.AddSingleton<IDbLockService, DbLockService>();
builder.Services.AddScoped<IPathDbService, PathDbService>();
builder.Services.AddScoped<IThaliakService, ThaliakService>();

builder.Services.AddQuartz(q =>
{
	q.ScheduleJob<UpdateJob>(trigger => trigger
		.WithIdentity("UpdateJob")
		.WithCronSchedule("0 30 0/4 * * ?")
		.StartNow());
	
	q.ScheduleJob<ExportJob>(trigger => trigger
		.WithIdentity("ExportJob")
		.WithCronSchedule("0 0 0/12 * * ?")
		.StartNow());
});
builder.Services.AddQuartzServer(q => q.WaitForJobsToComplete = true);

builder.Services.AddMetricServer(options =>
{
	options.Url = "/metrics";
	options.Port = 9184;
});
	
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
	app.UseHttpsRedirection();
}
else
{
	
}

Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(Console.Out));

var exportDir = app.Configuration["ExportDirectory"];
if (!Path.IsPathFullyQualified(exportDir))
	exportDir = Path.GetFullPath(exportDir);
if (!Directory.Exists(exportDir))
	Directory.CreateDirectory(exportDir);

app.UseFileServer(new FileServerOptions
{
	FileProvider = new PhysicalFileProvider(exportDir),
	RequestPath = "/download",
	EnableDirectoryBrowsing = false,
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.Run();