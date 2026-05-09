using System.Text.Json.Serialization;

namespace TravAi.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public ApiResponse() { }

        public ApiResponse(T data, string message = null)
        {
            Success = true;
            Message = message ?? "Operation completed successfully.";
            Data = data;
            Errors = null;
        }

        public ApiResponse(string message)
        {
            Success = true;
            Message = message;
            Data = default;
            Errors = null;
        }

        public ApiResponse(bool success, string message, List<string> errors = null)
        {
            Success = success;
            Message = message;
            Data = default;
            Errors = errors;
        }
    }
}
