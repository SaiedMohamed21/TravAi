using System.Net;
using System.Text.Json;
using UserAuthorizationandAuthentication.DTOs;

namespace UserAuthorizationandAuthentication.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = _env.IsDevelopment()
                    ? new ApiResponse<string>(false, ex.Message, new List<string> { ex.StackTrace?.ToString() })
                    : new ApiResponse<string>(false, "Internal Server Error. Please contact support.", null);
                
                // Handle specific exceptions if needed (e.g. key not found -> 404)
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)) 
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = ex.Message;
                }
                else if (ex.Message.Contains("registered", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = ex.Message;
                }

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
