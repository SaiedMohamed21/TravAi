using System.Net;
using System.Text.Json;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;
using UserAuthorizationandAuthentication.Models.Common;

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
                
                var response = new ApiResponse<string>(
                    success: false, 
                    message: ex.Message, 
                    errors: new List<string>()
                );

                if (ex is AppException appEx)
                {
                    context.Response.StatusCode = (int)appEx.StatusCode;
                }
                else if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)) 
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                else if (ex.Message.Contains("registered", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    if (_env.IsDevelopment())
                    {
                        response.Errors.Add(ex.InnerException?.Message ?? "No Inner Exception");
                        response.Errors.Add(ex.StackTrace ?? "");
                    }
                }

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
