using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartX.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "SmartX-IoT-Mesh Gateway API",
        Version = "v1",
        Description = "Edge Data Ingestion & Telemetry Minimal API Endpoint"
    });
});

// Configure CORS for desktop & web client integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartX API v1");
    c.RoutePrefix = "swagger";
});

// ------------------------------------------------------------------
// ENDPOINT 1: POST /api/telemetry
// Accepts TelemetryPacket<double> and evaluates alert thresholds
// ------------------------------------------------------------------
app.MapPost("/api/telemetry", (TelemetryPacket<double> packet) =>
{
    if (packet == null)
    {
        return Results.BadRequest(new { Status = "ERROR", Message = "Telemetry packet cannot be null." });
    }

    // Dynamic Visual State Evaluation: RED_ALERT if value > 80.0 else GREEN_NORMAL
    string visualState = packet.PayloadValue > 80.0 ? "RED_ALERT" : "GREEN_NORMAL";
    string alertMsg = visualState == "RED_ALERT" 
        ? $"CRITICAL ALERT: Reading {packet.PayloadValue:F2} exceeds 80.0 threshold on node {packet.MacAddress}."
        : $"NORMAL OPERATION: Reading {packet.PayloadValue:F2} within nominal range on node {packet.MacAddress}.";

    var response = new TelemetryResponse
    {
        Status = "SUCCESS",
        VisualState = visualState,
        MacAddress = packet.MacAddress,
        Location = packet.Location,
        Category = packet.Category,
        Value = packet.PayloadValue,
        ProcessedAt = DateTime.UtcNow,
        Message = alertMsg
    };

    return Results.Ok(response);
})
.WithName("SubmitTelemetry");

// ------------------------------------------------------------------
// ENDPOINT 2: POST /api/sensors/{mac}/upload-log
// Handles multipart file uploads (IFormFile) saving to UploadedLogs
// ------------------------------------------------------------------
app.MapPost("/api/sensors/{mac}/upload-log", async (string mac, IFormFile file, IWebHostEnvironment env) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { Status = "ERROR", Message = "No file attached or file is empty." });
    }

    // Ensure UploadedLogs directory exists
    string uploadDir = Path.Combine(env.ContentRootPath, "UploadedLogs");
    if (!Directory.Exists(uploadDir))
    {
        Directory.CreateDirectory(uploadDir);
    }

    // Sanitize and construct safe file path
    string safeMac = mac.Replace(":", "-").Replace("/", "_");
    string timestampPrefix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    string safeFileName = $"{safeMac}_{timestampPrefix}_{Path.GetFileName(file.FileName)}";
    string destinationPath = Path.Combine(uploadDir, safeFileName);

    using (var stream = new FileStream(destinationPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new
    {
        Status = "SUCCESS",
        MacAddress = mac,
        OriginalFileName = file.FileName,
        SavedFileName = safeFileName,
        FileSizeBytes = file.Length,
        UploadedAt = DateTime.UtcNow,
        FilePath = destinationPath
    });
})
.WithName("UploadSensorLog")
.DisableAntiforgery(); // Permissive for API file uploads

app.Run();
