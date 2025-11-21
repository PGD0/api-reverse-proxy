using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

var baseUrl = Environment.GetEnvironmentVariable("PRIVATE_API_URL");
var apiKey = Environment.GetEnvironmentVariable("PRIVATE_API_KEY");

var proxyOverrides = new Dictionary<string, string?>
{
    ["ReverseProxy:Clusters:resourceCluster:Destinations:destination1:Address"] = baseUrl
};

builder.Configuration.AddInMemoryCollection(proxyOverrides);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            transformContext.ProxyRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        });
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
app.Use(async (context, next) =>
{
    // Custom middleware logic can be added here
    var origin = context.Request.Headers["Origin"].ToString();
    var referer = context.Request.Headers["Referer"].ToString();
    var userAgent = context.Request.Headers["User-Agent"].ToString();

    if (!origin.Contains("azure.gestech.com.co") ||
        !referer.StartsWith("https://azure.gestech.com.co") ||
        string.IsNullOrEmpty(userAgent))
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
        return;
    }

    await next();
});
//app.MapGet("/", () => "Hello World!");

app.MapReverseProxy();

app.Run();