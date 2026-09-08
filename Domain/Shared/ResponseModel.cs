using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Shared
{
    public class ResponseModel<T>
    {
        public T Data { get; set; }
        public int TotalCount { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessege { get; set; }

        public ResponseModel(bool isSuccess = false, int totalCount = 0, T data = default, string errorMessage = null)
        {
            this.IsSuccess = isSuccess;
            if (errorMessage == null)
                this.ErrorMessege = isSuccess ? null : "Error";
            else
                this.ErrorMessege = errorMessage;
            this.Data = data;
            TotalCount = totalCount;
        }

        public static ResponseModel<T> Success(T data, int totalCount) => new(isSuccess: true, totalCount: totalCount, data: data, errorMessage: null);
        public static ResponseModel<T> Success() => new(isSuccess: true, totalCount: 0, data: default, errorMessage: null);
        public static ResponseModel<T> Success(T data) => new(isSuccess: true, totalCount: 0, data: data, errorMessage: null);
        public new static ResponseModel<T> Failure(string error) => new(false, errorMessage: error);

    }
}
