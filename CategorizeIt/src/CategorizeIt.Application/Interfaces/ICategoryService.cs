using CategorizeIt.Application.Models.Categories;

namespace CategorizeIt.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
}