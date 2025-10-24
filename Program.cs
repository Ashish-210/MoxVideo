using log4net;
using log4net.Config;
using MoxVideo.Service;
using MoxVideo.Service.HelperService;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<KeySetting>(appSettingsSection);
var appSettings = appSettingsSection.Get<KeySetting>();


var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("web.config"));

builder.Services.AddHttpClient("ElevenLabs", (serviceProvider,client) =>
{
    client.BaseAddress = new Uri("https://api.elevenlabs.io/");
    client.DefaultRequestHeaders.Add("xi-api-key", appSettings.Key);
});
builder.Services.AddHttpClient("MoxWave",(serviceProvider, client) =>
{
    client.BaseAddress = new Uri(appSettings.WaveUrl);
    client.DefaultRequestHeaders.Add("xi-api-key", appSettings.WaveKey);
});
builder.Services.AddSignalR(); // ✅ required for SignalR
builder.Services.AddSingleton<FfMpegWrapper>(); // our service
builder.Services.AddSingleton<TextTranslate>(); // our service
builder.Services.AddSingleton<ElevenLabsCloningService>();
builder.Services.AddSingleton<ElevenLabsSpeechToText>(); // our service
builder.Services.AddSingleton<LanguageCodeHelper>(); //our LanguageCode service
builder.Services.AddSingleton<YouTubeDownloader>(); // our service

builder.Services.AddSingleton<AudioExtractionHandler>();
builder.Services.AddSingleton<TranscriptionHandler>();
builder.Services.AddSingleton<VttCreationHandler>();
builder.Services.AddSingleton<TransalationHandlerService>();
builder.Services.AddSingleton<VideoCreationHandler>();
builder.Services.AddSingleton<TranslationService>();
// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(videoPath),
    RequestPath = "/Uploads",
    ServeUnknownFileTypes = true // so .mp4 is served correctly
});
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<DownloadHub>("/downloadHub");

app.Run();
