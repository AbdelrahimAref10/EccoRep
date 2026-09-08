using Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Presentation.Response;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Presentation.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment,
            IDateTimeProvider dateTimeProvider)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. RequestId: {RequestId}", 
                    context.TraceIdentifier);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var message = "An error occurred while processing your request.";

            // Handle specific exception types
            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    code = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;
                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    message = "You are not authorized to perform this action.";
                    break;
                case KeyNotFoundException:
                    code = HttpStatusCode.NotFound;
                    message = "The requested resource was not found.";
                    break;
                case InvalidOperationException:
                    code = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;
            }

            var response = new ErrorResponse
            {
                StatusCode = (int)code,
                Message = message,
                RequestId = context.TraceIdentifier,
                Timestamp = _dateTimeProvider.Now
            };

            // Only include stack trace in development
            if (_environment.IsDevelopment())
            {
                response.Details = exception.ToString();
            }

            var result = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}

