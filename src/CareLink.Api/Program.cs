using CareLink.Api.Data;
using CareLink.Api.Infrastructure;
using CareLink.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CareLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CareLink")));
builder.Services.AddSingleton<IDateProvider, SystemDateProvider>();
builder.Services.AddScoped<IFollowUpQueryService, FollowUpQueryService>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
