using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class CategoriesController(ICategoryService categories) : AdminControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index() => View(await categories.GetAllAsync(includeInactive: true));

        [HttpGet]
        public IActionResult Create() => View("Form", new CategoryFormViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);
            try
            {
                var created = await categories.CreateAsync(model.ToRequest());
                FlashSuccess($"Đã thêm danh mục \"{created.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                AddErrors(ex);
                return View("Form", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                return View("Form", CategoryFormViewModel.From(await categories.GetAsync(id)));
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            model.Id = id;
            if (!ModelState.IsValid) return View("Form", model);
            try
            {
                var updated = await categories.UpdateAsync(id, model.ToRequest());
                FlashSuccess($"Đã cập nhật danh mục \"{updated.Name}\".");
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }
            catch (ApiException ex)
            {
                AddErrors(ex);
                return View("Form", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await categories.GetAsync(id);
                await categories.DeleteAsync(id);
                FlashSuccess($"Đã xóa danh mục \"{category.Name}\".");
            }
            catch (ApiException ex)
            {
                FlashError(ex.Message);   // ví dụ: danh mục còn món => không cho xóa
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
