using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public record DbResult
    {
        public bool IsSuccess { get; init; }
        public string ErrorMessage { get; init; }



        public static explicit operator Result(DbResult dbResult)
        {
            if (dbResult.IsSuccess)
                return Result.Success();
            else
                return Result.Failure(dbResult.ErrorMessage);
        }
    }
}
