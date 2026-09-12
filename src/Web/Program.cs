using Web.Components;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// GitHub Projects dashboard: bind configuration (owner/token) and fall back to the GITHUB_TOKEN
// environment variable when no token is supplied via configuration/user-secrets.
builder.Services
    .AddOptions<GitHubProjectsOptions>()
    .Bind(builder.Configuration.GetSection(GitHubProjectsOptions.SectionName));
builder.Services.PostConfigure<GitHubProjectsOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.Token))
    {
        options.Token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }
});

builder.Services.AddHttpClient(GitHubRestClient.HttpClientName);
builder.Services.AddSingleton<IGitHubRestClient, GitHubRestClient>();
builder.Services.AddScoped<IGitHubProjectsService, GitHubProjectsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
