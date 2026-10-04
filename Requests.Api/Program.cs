using FluentValidation;
using Microsoft.OpenApi.Models;
using Requests.Api.Swagger;
using Requests.Api.Validators;
using Requests.Infrastructure;
using Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Requests API", 
        Version = "v1",
        Description = "API לחיפוש וסינון בקשות עם תמיכה בהרשאות"
    });
    
    // הוספת Headers לכל הקריאות ב-Swagger
    c.AddSecurityDefinition("UserHeaders", new OpenApiSecurityScheme
    {
        Description = "הזן User-Id (מספר) ו-Is-Admin (true/false) ב-Headers",
        Name = "X-User-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    
    // הוספת פרמטרים גלובליים
    c.OperationFilter<AddHeaderParameters>();
});
builder.Services.AddInfrastructure();
builder.Services.AddValidatorsFromAssemblyContaining<SearchRequestQueryValidator>();

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();

public partial class Program { }
