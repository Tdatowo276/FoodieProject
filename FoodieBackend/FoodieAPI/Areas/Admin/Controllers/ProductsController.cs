using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class ProductsController(
        IProductService products, ICategoryService categories, IImageStorage images) : AdminControllerBase
    {
        // GET /Admin/Products?keyword=&categoryId=&page=
        // Gọi bằng AJAX (lọc, chuyển trang) => chỉ trả về bảng (partial view)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ProductQuery query)
        {
            query = RememberQuery("Products", query);
            var model = new ProductListViewModel
            {
                Query = query,
                Result = await products.SearchAsync(query, includeHidden: true),
                Categories = await categories.GetAllAsync(includeInactive: true)
            };
            return IsAjax ? PartialView("_ProductTable", model) : View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create() =>
            View("Form", await WithCategoriesAsync(new ProductFormViewModel()));

        [HttpPost]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", await WithCategoriesAsync(model));

            var uploaded = await TryUploadAsync(model);
            if (!ModelState.IsValid) return View("Form", await WithCategoriesAsync(model));

            try
            {
                var created = await products.CreateAsync(model.ToRequest(uploaded ?? NullIfEmpty(model.LibraryImage)));
                FlashSuccess($"Đã thêm món \"{created.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                images.Delete(uploaded);   // lưu thất bại => không để lại ảnh mồ côi
                AddErrors(ex, ImageFieldMap);
                return View("Form", await WithCategoriesAsync(model));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await products.GetAsync(id, includeHidden: true);
                return View("Form", await WithCategoriesAsync(ProductFormViewModel.From(product)));
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            ProductDto existing;
            try
            {
                existing = await products.GetAsync(id, includeHidden: true);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }

            model.Id = id;
            model.CurrentImageUrl = existing.ImageUrl;   // đọc lại từ CSDL, không tin hidden field
            if (!ModelState.IsValid) return View("Form", await WithCategoriesAsync(model));

            var uploaded = await TryUploadAsync(model);
            if (!ModelState.IsValid) return View("Form", await WithCategoriesAsync(model));

            // Ảnh upload mới > ảnh chọn từ thư viện > xóa ảnh > giữ ảnh cũ.
            // Service kiểm tra ảnh có thật, và tự xóa file upload cũ nếu không còn dùng (ảnh thư viện không bao giờ bị xóa).
            var imageUrl = uploaded ?? NullIfEmpty(model.LibraryImage) ?? (model.RemoveImage ? null : existing.ImageUrl);

            try
            {
                var updated = await products.UpdateAsync(id, model.ToRequest(imageUrl));
                FlashSuccess($"Đã cập nhật món \"{updated.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                images.Delete(uploaded);
                AddErrors(ex, ImageFieldMap);
                return View("Form", await WithCategoriesAsync(model));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await products.GetAsync(id, includeHidden: true);
                await products.DeleteAsync(id);
                FlashSuccess($"Đã xóa món \"{product.Name}\". Các đơn hàng cũ vẫn giữ tên và giá món.");
            }
            catch (ApiException ex)
            {
                FlashError(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Gắn ảnh trong thư mục images/products cho các món chưa có ảnh, theo tên món.</summary>
        [HttpPost]
        public async Task<IActionResult> AssignLibraryImages()
        {
            var count = await products.AssignLibraryImagesAsync();
            if (count > 0)
                FlashSuccess($"Đã gắn ảnh cho {count} món.");
            else
                FlashError("Không có món nào được gắn ảnh. Kiểm tra tên file trong images/products có khớp tên món (không dấu, nối bằng gạch ngang) và món đó chưa có ảnh.");
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------------

        // Lỗi ảnh từ Service (key "imageUrl") hiện dưới ô chọn ảnh thư viện
        private static readonly Dictionary<string, string> ImageFieldMap = new() { ["imageUrl"] = nameof(ProductFormViewModel.LibraryImage) };

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>Lưu ảnh nếu có chọn file; lỗi (sai định dạng, quá 2MB...) gắn vào ô ImageFile.</summary>
        private async Task<string?> TryUploadAsync(ProductFormViewModel model)
        {
            if (model.ImageFile is null || model.ImageFile.Length == 0) return null;
            try
            {
                return await images.SaveAsync(model.ImageFile, HttpContext.RequestAborted);
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return null;
            }
        }

        private async Task<ProductFormViewModel> WithCategoriesAsync(ProductFormViewModel model)
        {
            var all = await categories.GetAllAsync(includeInactive: true);
            // Danh mục đang ẩn chỉ hiện nếu món hiện tại đang thuộc danh mục đó
            model.Categories = all
                .Where(c => c.IsActive || c.Id == model.CategoryId)
                .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (đang ẩn)", c.Id.ToString()))
                .ToList();
            model.LibraryImages = images.ListLibrary()
                .Select(url => new SelectListItem(Path.GetFileName(url), url))
                .ToList();
            return model;
        }
    }
}
