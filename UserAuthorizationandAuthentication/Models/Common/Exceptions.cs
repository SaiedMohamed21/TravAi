using System;
using System.Net;

namespace UserAuthorizationandAuthentication.Models.Common
{
    public class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, HttpStatusCode.NotFound) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(message, HttpStatusCode.Unauthorized) { }
    }
}
