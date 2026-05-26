namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Generic wrapper for all service operation results.
    /// Provides a consistent return type across the entire BLL layer.
    /// </summary>
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public IReadOnlyList<string> Errors { get; private set; } = [];
        public int StatusCode { get; private set; }

        private ServiceResult() { }

        public static ServiceResult<T> Success(T data, int statusCode = 200) =>
            new() { IsSuccess = true, Data = data, StatusCode = statusCode };

        public static ServiceResult<T> Created(T data) =>
            new() { IsSuccess = true, Data = data, StatusCode = 201 };

        public static ServiceResult<T> Failure(string errorMessage, int statusCode = 400) =>
            new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };

        public static ServiceResult<T> Failure(IEnumerable<string> errors, int statusCode = 400) =>
            new() { IsSuccess = false, Errors = errors.ToList().AsReadOnly(), ErrorMessage = string.Join("; ", errors), StatusCode = statusCode };

        public static ServiceResult<T> NotFound(string message = "Resource not found") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 404 };

        public static ServiceResult<T> Unauthorized(string message = "Unauthorized access") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 401 };

        public static ServiceResult<T> Forbidden(string message = "Access denied") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 403 };

        public static ServiceResult<T> Conflict(string message) =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 409 };
    }

    /// <summary>
    /// Non-generic variant for void operations (e.g. delete, update with no return).
    /// </summary>
    public class ServiceResult
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }
        public IReadOnlyList<string> Errors { get; private set; } = [];
        public int StatusCode { get; private set; }

        private ServiceResult() { }

        public static ServiceResult Success(int statusCode = 200) =>
            new() { IsSuccess = true, StatusCode = statusCode };

        public static ServiceResult Failure(string errorMessage, int statusCode = 400) =>
            new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };

        public static ServiceResult Failure(IEnumerable<string> errors, int statusCode = 400) =>
            new() { IsSuccess = false, Errors = errors.ToList().AsReadOnly(), ErrorMessage = string.Join("; ", errors), StatusCode = statusCode };

        public static ServiceResult NotFound(string message = "Resource not found") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 404 };

        public static ServiceResult Unauthorized(string message = "Unauthorized access") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 401 };

        public static ServiceResult Forbidden(string message = "Access denied") =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 403 };

        public static ServiceResult Conflict(string message) =>
            new() { IsSuccess = false, ErrorMessage = message, StatusCode = 409 };
    }
}
