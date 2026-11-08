namespace FoodieAPI.Common
{
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
    }

    public sealed class Validator
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public Validator Check(bool valid, string field, string message)
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