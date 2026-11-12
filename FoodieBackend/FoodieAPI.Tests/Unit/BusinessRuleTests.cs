using FoodieAPI.Common;
using FoodieAPI.Services;

namespace FoodieAPI.Tests.Unit
{
    /// <summary>Quy tắc nghiệp vụ thuần (không cần CSDL).</summary>
    public class OrderStatusTests
    {
        [Theory]
        [InlineData(OrderStatuses.Pending, OrderStatuses.Preparing)]
        [InlineData(OrderStatuses.Pending, OrderStatuses.Cancelled)]
        [InlineData(OrderStatuses.Preparing, OrderStatuses.Delivering)]
        [InlineData(OrderStatuses.Preparing, OrderStatuses.Cancelled)]
        [InlineData(OrderStatuses.Delivering, OrderStatuses.Completed)]
        public void CanChange_ValidFlow_ReturnsTrue(string from, string to) =>
            Assert.True(OrderStatuses.CanChange(from, to));

        [Theory]
        [InlineData(OrderStatuses.Pending, OrderStatuses.Delivering)]     // nhảy cóc
        [InlineData(OrderStatuses.Pending, OrderStatuses.Completed)]
        [InlineData(OrderStatuses.Delivering, OrderStatuses.Cancelled)]   // đang giao không hủy được
        [InlineData(OrderStatuses.Completed, OrderStatuses.Pending)]      // trạng thái cuối
        [InlineData(OrderStatuses.Cancelled, OrderStatuses.Pending)]
        [InlineData(OrderStatuses.Preparing, OrderStatuses.Pending)]      // không lùi bước
        public void CanChange_InvalidFlow_ReturnsFalse(string from, string to) =>
            Assert.False(OrderStatuses.CanChange(from, to));

        [Fact]
        public void NextOf_ReturnsAllowedNextStatuses()
        {
            Assert.Equal([OrderStatuses.Preparing, OrderStatuses.Cancelled], OrderStatuses.NextOf(OrderStatuses.Pending));
            Assert.Empty(OrderStatuses.NextOf(OrderStatuses.Completed));
            Assert.Empty(OrderStatuses.NextOf(OrderStatuses.Cancelled));
        }

        [Theory]
        [InlineData("pending", true)]
        [InlineData("completed", true)]
        [InlineData("PENDING", false)]
        [InlineData("hacked", false)]
        [InlineData(null, false)]
        public void IsValid(string? status, bool expected) => Assert.Equal(expected, OrderStatuses.IsValid(status));
    }

    public class ShippingFeeTests
    {
        [Theory]
        [InlineData(0, 15_000)]
        [InlineData(199_999, 15_000)]
        [InlineData(200_000, 0)]          // từ 200.000đ được miễn phí giao hàng
        [InlineData(1_000_000, 0)]
        public void ShippingFeeFor_AppliesFreeShippingThreshold(long subtotal, long expectedFee) =>
            Assert.Equal(expectedFee, OrderService.ShippingFeeFor(subtotal));
    }

    public class TextUtilTests
    {
        [Theory]
        [InlineData("Phở Bò Tái", "pho bo tai")]
        [InlineData("Đồ uống", "do uong")]
        [InlineData("  Gà Rán  ", "ga ran")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Normalize_RemovesDiacriticsAndLowercases(string? input, string expected) =>
            Assert.Equal(expected, TextUtil.Normalize(input));

        [Theory]
        [InlineData("Combo Burger Gà Giòn", "combo-burger-ga-gion")]
        [InlineData("  Mỳ  Ý!! ", "my-y")]
        [InlineData("Sushi tổng hợp 12 miếng", "sushi-tong-hop-12-mieng")]
        public void Slug_CreatesSafeFileName(string input, string expected) =>
            Assert.Equal(expected, TextUtil.Slug(input));

        [Fact]
        public void Keyword_EmptyIsNull_LongIsTruncatedTo100()
        {
            Assert.Null(TextUtil.Keyword("   "));
            Assert.Equal(100, TextUtil.Keyword(new string('a', 500))!.Length);
        }
    }

    public class PagingTests
    {
        [Theory]
        [InlineData(null, null, 1, 10, 0)]     // mặc định
        [InlineData(3, 20, 3, 20, 40)]
        [InlineData(0, 1000, 1, 50, 0)]        // trang tối thiểu 1, tối đa 50 dòng / trang
        [InlineData(-5, 0, 1, 1, 0)]
        public void Normalize_ClampsValues(int? page, int? size, int expPage, int expSize, int expSkip) =>
            Assert.Equal((expPage, expSize, expSkip), Paging.Normalize(page, size));
    }

    public class ImageSnifferTests
    {
        [Fact]
        public void Detect_RecognizesRealPngAndJpg()
        {
            Assert.Equal("png", ImageSniffer.Detect(Infrastructure.TestHelpers.PngBytes));
            Assert.Equal("jpg", ImageSniffer.Detect(Infrastructure.TestHelpers.JpgBytes));
        }

        [Fact]
        public void Detect_RejectsFakeOrOtherFormats()
        {
            Assert.Null(ImageSniffer.Detect("GIF89a"u8.ToArray()));
            Assert.Null(ImageSniffer.Detect("<?php echo 1; ?>"u8.ToArray()));   // file đổi đuôi thành .png
            Assert.Null(ImageSniffer.Detect([0x89, 0x50]));                      // quá ngắn
        }
    }

    public class ImagePathPatternTests
    {
        [Theory]
        [InlineData("/uploads/0123456789abcdef0123456789abcdef.png")]
        [InlineData("/uploads/0123456789abcdef0123456789abcdef.jpg")]
        [InlineData("/images/products/burger-ga.png")]
        [InlineData("/images/products/combo-2-burger.webp")]
        public void ProductImagePattern_AcceptsSystemPaths(string url) =>
            Assert.Matches(Patterns.ProductImagePattern, url);

        [Theory]
        [InlineData("/images/products/../../appsettings.json")]     // path traversal
        [InlineData("/uploads/../Program.cs")]
        [InlineData("https://evil.example.com/a.png")]               // link ngoài
        [InlineData("/images/products/Burger Ga.png")]               // chữ hoa, khoảng trắng
        [InlineData("/images/products/burger-gà.png")]               // có dấu
        [InlineData("/uploads/abc.png")]                             // không phải GUID
        [InlineData("/images/products/script.svg")]                  // định dạng không cho phép
        public void ProductImagePattern_RejectsUnsafePaths(string url) =>
            Assert.DoesNotMatch(Patterns.ProductImagePattern, url);
    }
}
