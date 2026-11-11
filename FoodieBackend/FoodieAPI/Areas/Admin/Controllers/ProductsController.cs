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
                var created = await products.CreateAsync(model.ToRequest(uploaded));
                FlashSuccess($"Đã thêm món \"{created.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                images.Delete(uploaded);   // lưu thất bại => không để lại ảnh mồ côi
                AddErrors(ex);
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

            // Ảnh mới > xóa ảnh > giữ ảnh cũ. Service tự xóa file ảnh cũ nếu không còn dùng.
            var imageUrl = uploaded ?? (model.RemoveImage ? null : existing.ImageUrl);

            try
            {
                var updated = await products.UpdateAsync(id, model.ToRequest(imageUrl));
                FlashSuccess($"Đã cập nhật món \"{updated.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                images.Delete(uploaded);
                AddErrors(ex);
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

        // ---------------------------------------------------------------------

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
            return model;
        }
    }
}
