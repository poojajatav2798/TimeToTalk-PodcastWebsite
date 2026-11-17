using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            HomePage.CONTENT_TYPE_NAME,
            AboutPage.CONTENT_TYPE_NAME,
            ContactPage.CONTENT_TYPE_NAME,
            ListingPage.CONTENT_TYPE_NAME,
            DetailPage.CONTENT_TYPE_NAME
        }
    });

    features.UseWebPageRouting();
});

builder.Services.AddAuthentication();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<ITiyaContentRepository, TiyaContentRepository>();
builder.Services.AddScoped<IEpisodeRepository, Repositories>();
builder.Services.AddScoped<IHostRepository, Repositories>();
builder.Services.AddScoped<ITopicRepository, Repositories>();

var app = builder.Build();
app.InitKentico();

app.UseStaticFiles();
app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

app.MapControllers();

app.Kentico().MapRoutes();

app.Run();
