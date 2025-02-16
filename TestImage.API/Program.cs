using ImageRendererNet9;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddImageRenderer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Test API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/upload", async (ImageProcessor imageProcessor, IFormFile file) =>
{

    var result = await imageProcessor.SaveImageToLocalAsync(file);
    return result;
})
.WithName("Upload")
.DisableAntiforgery();

app.MapPost("/uploadmulti", async (ImageProcessor imageProcessor, [FromForm] IEnumerable<IFormFile> files) =>
{

    var result = await imageProcessor.SaveImageToLocalAsync(files);
    return result;
})
.WithName("UploadMulti")
.DisableAntiforgery();

app.MapDelete("/delete", async (ImageProcessor imageProcessor, string path) =>
{

    var result = await imageProcessor.DeleteImageAsync(path);
    return result;
})
.WithName("Delete")
.DisableAntiforgery();

app.MapDelete("/deletemulti", async (ImageProcessor imageProcessor, IEnumerable<string> paths) =>
{

    var result = await imageProcessor.DeleteImagesAsync(paths);
    return result;
})
.WithName("DeleteMulti")
.DisableAntiforgery();

app.Run();
