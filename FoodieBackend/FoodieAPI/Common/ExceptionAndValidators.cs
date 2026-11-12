namespace FoodieAPI.Common
{
    /// <summary>
    /// Lỗi nghiệp vụ có mã HTTP đi kèm. Service ném ra; API trả về ProblemDetails
    /// (ErrorHandlingMiddleware), còn MVC chuyển thành lỗi ModelState / TempData.
    /// </summary>
    public sealed class ApiException : Exception
    {
        public int StatusCode { get; }
        public IDictionary<string, string[]>? Errors { get; }

        public ApiException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
            : base(message)
        {
            StatusCode = statusCode;
            Errors = errors;
        }

        public static ApiException Validation(string field, string message) =>
            new(400, "Dữ liệu không hợp lệ.", new Dictionary<string, string[]> { [field] = [message] });

        public static ApiException NotFound(string message) => new(404, message);

        public static ApiException Conflict(string message) => new(409, message);
    }

    /// <summary>
    /// Gom các lỗi quy tắc nghiệp vụ (những gì Data Annotations không diễn đạt được,
    /// ví dụ kiểm tra lại sau khi đã Trim) rồi ném một ApiException duy nhất.
    /// </summary>
    public sealed class RuleChecker
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public RuleChecker Check(bool valid, string field, string message)
        {
            if (valid) return this;

            if (!_errors.TryGetValue(field, out var list))
            {
                list = [];
                _errors[field] = list;
            }
            list.Add(message);
            return this;
        }

        public void ThrowIfInvalid()
        {
            if (_errors.Count == 0) return;
            throw new ApiException(400, "Dữ liệu không hợp lệ.",
                _errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
