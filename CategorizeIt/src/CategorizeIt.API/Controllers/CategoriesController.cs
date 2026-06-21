using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categories;

    public CategoriesController(ICategoryRepository categories)
    {
        _categories = categories;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categories.GetAllAsync();

        var result = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            IsSystem = c.IsSystem,
        });

        return Ok(result);
    }
}