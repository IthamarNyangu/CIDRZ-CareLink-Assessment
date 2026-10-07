using CareLink.Api.Data;
using CareLink.Api.Infrastructure;
using CareLink.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CareLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CareLink")));
builder.Services.AddSingleton<IDateProvider, SystemDateProvider>();
builder.Services.AddScoped<IFollowUpQueryService, FollowUpQueryService>();

var app = builder.Build();

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
