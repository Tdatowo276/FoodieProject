using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FoodieAPI.Common
{
    /// <summary>
    /// Kiểm tra Data Annotations ([Required], [StringLength], [Range]...) trên DTO.
    /// Minimal API không tự làm việc này như MVC (ModelState), nên dùng qua <see cref="ValidationFilter{T}"/>.
    /// Lỗi trả về dạng ProblemDetails 400 với key camelCase, ví dụ "items[0].quantity".
    /// </summary>
    public static class DataValidation
    {
        public static void ThrowIfInvalid(object model)
        {
            var errors = new Dictionary<string, List<string>>();
            Collect(model, prefix: "", errors);

            if (errors.Count > 0)
                throw new ApiException(400, "Dữ liệu không hợp lệ.",
                    errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        private static void Collect(object model, string prefix, Dictionary<string, List<string>> errors)
        {
            var results = new List<ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                model, new ValidationContext(model), results, validateAllProperties: true);

            foreach (var r in results)
            {
                var members = r.MemberNames.Any() ? r.MemberNames : [""];
                foreach (var member in members)
                {
                    var key = prefix + JsonNamingPolicy.CamelCase.ConvertName(member);
                    if (!errors.TryGetValue(key, out var list)) errors[key] = list = [];
                    list.Add(r.ErrorMessage ?? "Giá trị không hợp lệ.");
                }
            }

            // Kiểm tra tiếp từng phần tử trong các thuộc tính dạng danh sách (ví dụ Items của đơn hàng)
            foreach (var prop in model.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                    continue;
                if (prop.GetValue(model) is not IEnumerable items) continue;

                var index = 0;
                foreach (var item in items)
                {
                    if (item is not null && !item.GetType().IsPrimitive)
                        Collect(item, $"{prefix}{JsonNamingPolicy.CamelCase.ConvertName(prop.Name)}[{index}].", errors);
                    index++;
                }
            }
        }
    }

    /// <summary>Endpoint filter: validate tham số kiểu T của endpoint trước khi chạy handler.</summary>
    public sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var model = context.Arguments.OfType<T>().FirstOrDefault()
                ?? throw new ApiException(400, "Thiếu dữ liệu gửi lên.");

            DataValidation.ThrowIfInvalid(model);
            return await next(context);
        }
    }

    public static class ValidationFilterExtensions
    {
        /// <summary>Bật kiểm tra Data Annotations cho body kiểu T của endpoint.</summary>
        public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder builder) where T : class =>
            builder.AddEndpointFilter<ValidationFilter<T>>();
    }
}
