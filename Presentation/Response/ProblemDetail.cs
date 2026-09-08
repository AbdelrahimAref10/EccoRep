using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Response
{
    public class ProblemDetail
    {
        public int statusCode { get; set; }
        public string errorMessage { get; set; } = string.Empty;
        public object additioanlData { get; set; } = string.Empty;

        public static ProblemDetail CreateProblemDetail(string ErrorMessage)
        {
            return new ProblemDetail
            {
                statusCode = StatusCodes.Status400BadRequest,
                errorMessage = ErrorMessage
            };
        }


        public static ProblemDetail CreateProblemDetail(string ErrorMessage, object additioanlData)
        {
            return new ProblemDetail
            {
                statusCode = StatusCodes.Status400BadRequest,
                errorMessage = ErrorMessage,
                additioanlData = additioanlData
            };
        }
    }
}
