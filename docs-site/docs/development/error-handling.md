---
id: error-handling
title: Error Handling & Result Pattern
---

The URL Shortener implements a comprehensive error handling strategy using the Result pattern for clean, predictable error management.

## Result Pattern Implementation

### Core Result Types

The application uses a custom Result pattern implementation located in `src/Domain/Result/`:

```csharp
// Base Result type
public abstract record Result<T>;

// Success case
public record Success<T>(T res) : Result<T>;

// Failure case  
public record Failure<T>(Error error) : Result<T>;

// Error details
public record Error(string message, ErrorCode code);
```

### Error Codes

```csharp
public enum ErrorCode
{
    BAD_REQUEST = 400,
    NOT_FOUND = 404, 
    INTERNAL_SERVER_ERROR = 500
}
```

## Usage Patterns

### Service Layer Implementation

```csharp
public async Task<Result<UrlMapping>> CreateUrlMappingAsync(UrlMapping urlMapping, string? customShortCode = null)
{
    try
    {
        // Validation
        if (urlMapping == null)
        {
            return new Failure<UrlMapping>(new Error("UrlMapping cannot be null.", ErrorCode.BAD_REQUEST));
        }

        // Business logic...
        var createdUrl = await _repository.AddAsync(urlMapping);
        
        if (createdUrl is Success<UrlMapping> success)
        {
            return success;
        }
        
        return new Failure<UrlMapping>(new Error("Creation failed", ErrorCode.INTERNAL_SERVER_ERROR));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating UrlMapping");
        return new Failure<UrlMapping>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
    }
}
```

### Controller Layer Handling

```csharp
[HttpPost]
public async Task<ActionResult<CreateUrlMappingResponse>> CreateShortUrl([FromBody] CreateUrlMappingRequest request)
{
    var urlMapping = new UrlMapping
    {
        OriginalUrl = request.OriginalUrl,
        // ... other properties
    };

    var result = await _urlMappingService.CreateUrlMappingAsync(urlMapping, request.CustomShortCode);
    
    // Pattern matching on Result type
    return result switch
    {
        Success<UrlMapping> success => CreatedAtAction(
            nameof(GetUrlById),
            new { id = success.res.Id },
            MapToResponse(success.res)
        ),
        Failure<UrlMapping> failure => StatusCode(
            (int)failure.error.code,
            failure.error.message
        ),
        _ => StatusCode(500, "Unexpected error")
    };
}
```

## Error Response Format

### Standard Error Response

All API endpoints return consistent error responses:

```json
{
  "status": 400,
  "message": "Custom short code must be 8 characters long.",
  "details": "The provided short code 'abc' has 3 characters but exactly 8 are required."
}
```

### HTTP Status Code Mapping

| ErrorCode | HTTP Status | Description | Example Scenario |
|-----------|-------------|-------------|------------------|
| `BAD_REQUEST` | 400 | Client request error | Invalid URL format, short code too short |
| `NOT_FOUND` | 404 | Resource not found | Short code doesn't exist |
| `INTERNAL_SERVER_ERROR` | 500 | Server error | Database connection failed |

## Validation Integration

### FluentValidation Setup

The application uses FluentValidation for request validation:

```csharp
// In Program.cs
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>();
```

### Example Validator

```csharp
public class CreateUrlMappingRequestValidator : AbstractValidator<CreateUrlMappingRequest>
{
    public CreateUrlMappingRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .WithMessage("Original URL is required")
            .Must(BeAValidUrl)
            .WithMessage("Must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.CustomShortCode)
            .Length(8)
            .When(x => !string.IsNullOrEmpty(x.CustomShortCode))
            .WithMessage("Custom short code must be exactly 8 characters");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
```

## Common Error Scenarios

### 1. URL Creation Errors

**Invalid URL Format:**
```json
{
  "status": 400,
  "message": "OriginalUrl must be a valid HTTP/HTTPS URL, It must begin with https:// Or http://"
}
```

**Short Code Already Exists:**
```json
{
  "status": 400,
  "message": "Custom short code 'mylink01' already exists."
}
```

**Expiration Date in Past:**
```json
{
  "status": 400,
  "message": "Expiration date must be in the future."
}
```

### 2. Retrieval Errors

**URL Not Found:**
```json
{
  "status": 404,
  "message": "URL with ID 123 not found."
}
```

**Short Code Not Found (Redirect):**
```json
{
  "status": 404,
  "message": "URL not found"
}
```

### 3. System Errors

**Database Connection Failed:**
```json
{
  "status": 500,
  "message": "A database error occurred while processing the request."
}
```

**Redis Cache Unavailable:**
```json
{
  "status": 500,
  "message": "Cache service temporarily unavailable."
}
```

## Logging Integration

### Structured Logging

All errors are logged with structured information:

```csharp
_logger.LogError(ex, "Error creating UrlMapping for {OriginalUrl} with custom code {CustomShortCode}", 
    urlMapping.OriginalUrl, customShortCode);
```

### Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| `Error` | System errors, exceptions | Database failures, unexpected errors |
| `Warning` | Business rule violations | Short code conflicts, expired URLs |
| `Information` | Successful operations | URL created, cache hit/miss |
| `Debug` | Detailed flow information | Cache operations, validation steps |

### Example Log Output

```
[2024-01-15 10:30:15.123] [ERR] Error creating UrlMapping for "https://example.com" with custom code "abc123": Custom short code 'abc123' already exists.
[2024-01-15 10:30:20.456] [INF] URL shortened successfully: "https://example.com" -> "def456" (ID: 123)
[2024-01-15 10:30:25.789] [WAR] Redirect attempt for inactive short code: "xyz789"
```

## Exception Handling Middleware

### Global Exception Handler

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ArgumentException => new { status = 400, message = exception.Message },
            KeyNotFoundException => new { status = 404, message = "Resource not found" },
            _ => new { status = 500, message = "An internal server error occurred" }
        };

        context.Response.StatusCode = response.status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## Best Practices

### ✅ Do's

1. **Use Result Pattern Consistently**
   ```csharp
   // Good
   public async Task<Result<UrlMapping>> GetByIdAsync(int id)
   {
       if (id <= 0)
           return new Failure<UrlMapping>(new Error("Invalid ID", ErrorCode.BAD_REQUEST));
       // ...
   }
   ```

2. **Log Errors with Context**
   ```csharp
   _logger.LogError(ex, "Failed to process request {RequestId} for user {UserId}", requestId, userId);
   ```

3. **Return Specific Error Messages**
   ```csharp
   return new Failure<UrlMapping>(new Error($"Short code '{shortCode}' already exists", ErrorCode.BAD_REQUEST));
   ```

4. **Handle Different Error Types**
   ```csharp
   return result switch
   {
       Success<T> success => Ok(success.res),
       Failure<T> failure => StatusCode((int)failure.error.code, failure.error.message),
       _ => StatusCode(500, "Unexpected error")
   };
   ```

### ❌ Don'ts

1. **Don't Expose Internal Details**
   ```csharp
   // Bad
   return new Error(ex.StackTrace, ErrorCode.INTERNAL_SERVER_ERROR);
   
   // Good  
   return new Error("An error occurred while processing your request", ErrorCode.INTERNAL_SERVER_ERROR);
   ```

2. **Don't Ignore Validation Errors**
   ```csharp
   // Bad
   var url = request.OriginalUrl; // No validation
   
   // Good
   if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
       return new Failure<UrlMapping>(new Error("Invalid URL format", ErrorCode.BAD_REQUEST));
   ```

3. **Don't Use Generic Error Messages**
   ```csharp
   // Bad
   return new Error("Error occurred", ErrorCode.BAD_REQUEST);
   
   // Good
   return new Error("Custom short code must be exactly 8 characters long", ErrorCode.BAD_REQUEST);
   ```

## Testing Error Scenarios

### Unit Test Examples

```csharp
[Test]
public async Task CreateUrlMapping_WithExistingShortCode_ReturnsFailure()
{
    // Arrange
    var existingShortCode = "existing1";
    _mockRepository.Setup(r => r.UrlExistsAsync(existingShortCode))
        .ReturnsAsync(new Success<bool>(true));

    // Act
    var result = await _service.CreateUrlMappingAsync(urlMapping, existingShortCode);

    // Assert
    Assert.IsInstanceOf<Failure<UrlMapping>>(result);
    var failure = result as Failure<UrlMapping>;
    Assert.AreEqual(ErrorCode.BAD_REQUEST, failure.error.code);
    StringAssert.Contains("already exists", failure.error.message);
}
```

### Integration Test Examples

```csharp
[Test]
public async Task POST_UrlShortener_WithInvalidUrl_Returns400()
{
    // Arrange
    var request = new CreateUrlMappingRequest
    {
        OriginalUrl = "not-a-valid-url"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/UrlShortener", request);

    // Assert
    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    var content = await response.Content.ReadAsStringAsync();
    StringAssert.Contains("valid HTTP/HTTPS URL", content);
}
```