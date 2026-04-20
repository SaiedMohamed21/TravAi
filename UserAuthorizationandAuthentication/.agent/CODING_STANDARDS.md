# API Response & Error Handling Standards

All API endpoints in this project must adhere to the following response structure to ensure professional and consistent messaging.

## 1. Standard Response Wrapper (`ApiResponse<T>`)

All Controllers must return data wrapped in `ApiResponse<T>`.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
}
```

## 2. Success Scenarios (200 OK)

Return `Ok(new ApiResponse<T>(data, message))`

```json
{
  "success": true,
  "message": "Operation successful.",
  "data": { ... },
  "errors": null
}
```

## 3. Failure Scenarios (400/401/404)

Return `BadRequest`, `Unauthorized`, or `NotFound` with `ApiResponse<object>`.

```json
{
  "success": false,
  "message": "Error description.",
  "data": null,
  "errors": null
}
```

## 4. Global Exception Handling

Do **not** return raw 500 pages. Use `ExceptionMiddleware` to catch unhandled exceptions and return a standard JSON response.

```json
{
  "success": false,
  "message": "Internal Server Error.",
  "data": null,
  "errors": ["Stack trace (Development only)"]
}
```
