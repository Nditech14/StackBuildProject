using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Application.AuthResponse
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; } = false;
        public string Message { get; set; } = default!;
        public int StatusCode { get; set; } = 200;

        protected ServiceResponse(bool isSuccess, string respMessage, T? respData, int respCode)
        {
            StatusCode = respCode;
            Success = isSuccess;
            Message = respMessage;
            Data = respData;
        }

        protected ServiceResponse(bool isSuccess, string respMessage, int respCode = 200)
        {
            StatusCode = respCode;
            Success = isSuccess;
            Message = respMessage;
        }

        public ServiceResponse()
        {
        }

        public static ServiceResponse<T> Successful(T? data, string message = "Success", int statusCode = 200)
        {
            return new ServiceResponse<T>(true, message, data, statusCode);
        }

        public static ServiceResponse<T> Failure(string message, int statusCode = 400)
        {
            return new ServiceResponse<T>(false, message, statusCode);
        }
    }



    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }


    public class ApiResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; } = new List<T>();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("recordsTotal")]
        public int RecordsTotal { get; set; }

        [JsonProperty("recordsFiltered")]
        public int RecordsFiltered { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("responseCode")]
        public int ResponseCode { get; set; } = 200;

        [JsonProperty("status")]
        public bool Status { get; set; } = true;
    }

}
