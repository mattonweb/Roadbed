# Roadbed.Net

HTTP client wrapper with automatic retry logic, compression support, authentication handling, and standardized response patterns.

## Overview

This library provides a robust HTTP client service built on top of `IHttpClientFactory` with automatic retry logic for transient failures, configurable timeouts, compression support, and strongly-typed responses. Perfect for building resilient API integrations.

## Installation
```bash
dotnet add package Roadbed.Net
```

## Quick Start

### 1. Register Services
```csharp
using Roadbed;

var builder = WebApplication.CreateBuilder(args);

// Automatically discovers and registers Roadbed.Net services
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
```

### 2. Make HTTP Requests
```csharp
using Roadbed.Net;

// Simple GET request
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/foo"),
    Method = HttpMethod.Get
};

NetHttpResponse<string> response = await NetHttpClient.MakeRequestAsync<string>(
    request,
    cancellationToken);

if (response.IsSuccessStatusCode)
{
    string data = response.Data;
    // Process data
}
```

## Key Features

### Automatic Retry with Exponential Backoff
```csharp
var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/foo"),
    Method = HttpMethod.Get,
    RetryPattern = new NetHttpRetryPattern
    {
        MaxAttempts = 3,              // 4 total attempts (initial + 3 retries)
        DelayMultiplierInSeconds = 5  // Delays: 5s, 25s, 125s
    }
};
```

**Automatically retries:** 503 Service Unavailable, 408/504 Timeouts, Network errors

### Authentication
```csharp
// Bearer token
Authentication = new NetHttpAuthentication
{
    AuthenticationType = NetHttpAuthenticationType.Bearer,
    Value = "your-access-token"
}

// Basic authentication
Authentication = new NetHttpAuthentication
{
    AuthenticationType = NetHttpAuthenticationType.Basic,
    Value = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))
}
```

### Custom Headers
```csharp
HttpHeaders = new List<NetHttpHeader>
{
    new NetHttpHeader("X-API-Key", "your-api-key"),
    new NetHttpHeader("X-Custom-Header", "custom-value")
}
```

### POST with JSON
```csharp
var payload = new { Name = "Test", Value = 123 };
string json = JsonConvert.SerializeObject(payload);

var request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://api.example.com/foo"),
    Method = HttpMethod.Post,
    Content = new StringContent(json, Encoding.UTF8, "application/json")
};
```

## Configuration Reference

### NetHttpRequest Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HttpEndPoint` | `Uri?` | `null` | Target URL |
| `Method` | `HttpMethod` | `GET` | HTTP method |
| `Content` | `HttpContent?` | `null` | Request body |
| `Authentication` | `NetHttpAuthentication?` | `null` | Auth configuration |
| `HttpHeaders` | `List<NetHttpHeader>` | Empty | Custom headers |
| `EnableCompression` | `bool` | `true` | GZip/Deflate support |
| `TimeoutInSecondsPerAttempt` | `int` | `15` | Timeout per attempt |
| `RetryPattern` | `NetHttpRetryPattern` | 3 attempts, 5s multiplier | Retry config |

### NetHttpResponse\<T\> Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccessStatusCode` | `bool` | `true` if 200-299 (not 404) |
| `Data` | `T` | Response body as type `T` |
| `Errors` | `List<string>` | Error messages |
| `HttpStatusCode` | `int` | HTTP status code |
| `HttpStatusCodeDescription` | `string?` | Status description |

## Complete Example
```csharp
using Roadbed.Net;
using Roadbed;
using Newtonsoft.Json;

public class FooApiRepository : BaseClassWithLogging<FooApiRepository>
{
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public FooApiRepository(IConfiguration configuration, ILoggerFactory factory)
        : base(factory)
    {
        _baseUrl = configuration["FooApi:BaseUrl"]!;
        _apiKey = configuration["FooApi:ApiKey"]!;
    }

    public async Task<FooDto?> ReadAsync(int id, CancellationToken cancellationToken = default)
    {
        string endpoint = $"{_baseUrl}/foo/{id}";
        
        this.LogDebug("Fetching foo from endpoint: {Endpoint}", endpoint);

        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri(endpoint),
            Method = HttpMethod.Get,
            HttpHeaders = new List<NetHttpHeader>
            {
                new NetHttpHeader("X-API-Key", _apiKey)
            },
            TimeoutInSecondsPerAttempt = 10,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = 3,
                DelayMultiplierInSeconds = 2
            }
        };

        NetHttpResponse<string> response = await NetHttpClient.MakeRequestAsync<string>(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            this.LogError(
                "API request failed. Status: {StatusCode} - {StatusDescription}",
                response.HttpStatusCode,
                response.HttpStatusCodeDescription);
            return null;
        }

        FooDto? result = JsonConvert.DeserializeObject<FooDto>(response.Data);

        if (result == null)
        {
            this.LogError("Failed to deserialize response");
            return null;
        }

        this.LogDebug("Successfully retrieved foo with ID {Id}", result.Id);
        return result;
    }

    public async Task<int?> CreateAsync(FooDto dto, CancellationToken cancellationToken = default)
    {
        string endpoint = $"{_baseUrl}/foo";
        string json = JsonConvert.SerializeObject(dto);

        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri(endpoint),
            Method = HttpMethod.Post,
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            HttpHeaders = new List<NetHttpHeader>
            {
                new NetHttpHeader("X-API-Key", _apiKey)
            }
        };

        NetHttpResponse<string> response = await NetHttpClient.MakeRequestAsync<string>(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            this.LogError("Failed to create foo. Status: {StatusCode}", response.HttpStatusCode);
            return null;
        }

        FooDto? result = JsonConvert.DeserializeObject<FooDto>(response.Data);
        return result?.Id;
    }
}
```

## Requirements

- .NET 10.0+
- Microsoft.Extensions.Http (IHttpClientFactory)
- Roadbed (core utilities and logging)

## Related Packages

- **Roadbed** - Core utilities, logging, and dependency injection