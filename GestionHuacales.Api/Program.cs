using GestionHuacales.Api.DAL;
using GestionHuacales.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var ConStr = builder.Configuration.GetConnectionString("sqlite");

builder.Services.AddDbContextFactory<Contexto>(o => o.UseSqlite(ConStr));
builder.Services.AddScoped<Contexto>();
builder.Services.AddScoped<EntradaHuacalesServices>();
builder.Services.AddScoped<PartidasService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
