using FoodieAPI.Common;
using FoodieAPI.DTOs;

namespace FoodieAPI.Tests.Unit
{
    /// <summary>Data Annotations trên DTO (qua DataValidation, giống ValidationFilter của Minimal API).</summary>
    public class DtoValidationTests
    {
        private static string[] ErrorFields(object model)
        {
            var ex = Record.Exception(() => DataValidation.ThrowIfInvalid(model));
            if (ex is null) return [];
            var api = Assert.IsType<ApiException>(ex);
            Assert.Equal(400, api.StatusCode);
            return api.Errors!.Keys.ToArray();
        }

        [Fact]
        public void RegisterRequest_Valid_Passes() =>
            Assert.Empty(ErrorFields(new RegisterRequest("Nguyễn Văn A", "a@test.vn", "matkhau123")));

        [Fact]
        public void RegisterRequest_Invalid_ReportsEachField() =>
            Assert.Equal(["email", "name", "password"], ErrorFields(new RegisterRequest("A", "khong-phai-email", "123")).Order());

        [Fact]
        public void OrderRequest_ValidatesNestedItems()
        {
            var req = new OrderRequest(
                [new OrderItemRequest(1, 2), new OrderItemRequest(2, 0)],
                "Người nhận", "0901234567", "1 Lê Lợi", null, "cod");

            Assert.Equal(["items[1].quantity"], ErrorFields(req));
        }

        [Fact]
        public void OrderRequest_RejectsEmptyCartBadPhoneAndUnknownPayment()
        {
            var req = new OrderRequest([], "Người nhận", "123", "1 Lê Lợi", null, "momo");
            Assert.Equal(["items", "paymentMethod", "phone"], ErrorFields(req).Order());
        }

        [Fact]
        public void ChangePasswordRequest_ConfirmMustMatch() =>
            Assert.Equal(["confirmPassword"], ErrorFields(new ChangePasswordRequest("cu123456", "moi123456", "khac123456")));

        [Fact]
        public void ProductRequest_RejectsPathTraversalImage()
        {
            var req = new ProductRequest("Món thử", 1, 50_000, null, "/images/products/../../appsettings.json");
            Assert.Equal(["imageUrl"], ErrorFields(req));
        }

        [Fact]
        public void FeedbackRequest_ValidatesTopicRatingAndLength()
        {
            var req = new FeedbackRequest("Khách", "a@test.vn", null, "hacked", 6, "ngắn");
            Assert.Equal(["message", "rating", "topic"], ErrorFields(req).Order());
        }

        [Fact]
        public void AddressRequest_RequiresVietnamesePhoneFormat()
        {
            Assert.Empty(ErrorFields(new AddressRequest("Người nhận", "+84901234567", "1 Lê Lợi")));
            Assert.Equal(["phone"], ErrorFields(new AddressRequest("Người nhận", "090123", "1 Lê Lợi")));
        }
    }
}
