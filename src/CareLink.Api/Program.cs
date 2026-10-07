using CareLink.Api.Data;
using CareLink.Api.Infrastructure;
using CareLink.Api.Middleware;
using CareLink.Api.Security;
using CareLink.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Console logging works consistently on developer machines, containers, and servers.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "static demonstration token",
        In = ParameterLocation.Header,
        Description = "Use manager-demo-token or clinic-0101-demo-token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = []
    });
});
builder.Services.AddDbContext<CareLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CareLink")));
builder.Services.AddSingleton<IDateProvider, SystemDateProvider>();
builder.Services.AddScoped<IFollowUpQueryService, FollowUpQueryService>();
builder.Services.AddSingleton<IFacilityAccessService, FacilityAccessService>();
builder.Services
    .AddAuthentication(StaticTokenAuthenticationOptions.Scheme)
    .AddScheme<StaticTokenAuthenticationOptions, StaticTokenAuthenticationHandler>(
        StaticTokenAuthenticationOptions.Scheme,
        options => builder.Configuration.GetSection("Authentication").Bind(options));
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CareLinkDbContext>();
    await dbContext.Database.MigrateAsync();

    if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
    {
        var dateProvider = scope.ServiceProvider.GetRequiredService<IDateProvider>();
        await DemoDataSeeder.SeedAsync(dbContext, dateProvider.Today);
    }
}

if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Demo data seeded successfully.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var problemDetailsService =
        httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails =
        {
            Status = httpContext.Response.StatusCode,
            Title = ReasonPhrases.GetReasonPhrase(httpContext.Response.StatusCode)
        }
    });
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
