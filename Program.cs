using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Podcast.Web.Services;
using TimeToTalk;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddKentico(features =>
{
    features.UsePageBuilder(new PageBuilderOptions
    {
        ContentTypeNames = new[]
        {
            HomePage.CONTENT_TYPE_NAME
        }
    });
});

builder.Services.AddAuthentication();
// builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEpisodeRepository, Repositories>();
builder.Services.AddScoped<IHostRepository, Repositories>();
builder.Services.AddScoped<ITopicRepository, Repositories>();

var app = builder.Build();
app.InitKentico();


app.UseStaticFiles();
app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

// app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Kentico().MapRoutes();
//app.MapGet("/", () => "The Podcast.Web site has not been configured yet.");

app.Run();
