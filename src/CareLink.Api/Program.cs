using CareLink.Api.Data;
using CareLink.Api.Infrastructure;
using CareLink.Api.Middleware;
using CareLink.Api.Security;
using CareLink.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Console logging works consistently on developer machines, containers, and servers.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetConnectionString("CareLink")
    ?? throw new InvalidOperationException("The CareLink connection string is required.");
var activeDatabaseName = Path.GetFileName(
    new SqliteConnectionStringBuilder(connectionString).DataSource);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CareLink API",
        Version = "v1",
        Description = $"Active local database: {activeDatabaseName}. " +
                      "Follow-up responses are facility-scoped, status-filtered and paginated."
    });
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
    options.UseSqlite(connectionString));
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

var volumeSeedArgument = args.FirstOrDefault(argument =>
    argument.StartsWith("--seed-volume=", StringComparison.OrdinalIgnoreCase));
int? volumePatientCount = null;
if (volumeSeedArgument is not null)
{
    var rawCount = volumeSeedArgument[(volumeSeedArgument.IndexOf('=') + 1)..];
    if (!int.TryParse(rawCount, out var parsedCount))
    {
        throw new ArgumentException("--seed-volume must contain a numeric patient count.");
    }

    volumePatientCount = parsedCount;
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CareLinkDbContext>();
    await dbContext.Database.MigrateAsync();

    if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
    {
        var dateProvider = scope.ServiceProvider.GetRequiredService<IDateProvider>();
        await DemoDataSeeder.SeedAsync(dbContext, dateProvider.Today);
    }

    if (volumePatientCount.HasValue)
    {
        var dateProvider = scope.ServiceProvider.GetRequiredService<IDateProvider>();
        await VolumeDataSeeder.SeedAsync(
            dbContext,
            volumePatientCount.Value,
            dateProvider.Today);
    }
}

if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase) ||
    volumePatientCount.HasValue)
{
    Console.WriteLine(
        volumePatientCount.HasValue
            ? $"Volume data seeded successfully: {volumePatientCount:N0} patients and {volumePatientCount * 4:N0} visits."
            : "Demo data seeded successfully.");
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
