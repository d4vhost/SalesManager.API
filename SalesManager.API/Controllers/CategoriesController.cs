using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Categories; // DTO que acabamos de crear
using SalesManager.UseCases.DTOs.Common; // Para PagedResultDto
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesManager.UseCases.Interfaces; // Para DbUpdateConcurrencyException

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger; // Para logging

        public CategoriesController(IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/Categories?searchTerm=...&pageNumber=...&pageSize=...
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<CategoryDto>>> GetCategories(
            [FromQuery] string searchTerm = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (categories, totalCount) = await _unitOfWork.CategoryRepository.FindCategoriesAsync(searchTerm, pageNumber, pageSize);

            var categoryDtos = categories.Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                Description = c.Description
            }).ToList();

            var pagedResult = new PagedResultDto<CategoryDto>(categoryDtos, pageNumber, pageSize, totalCount);
            return Ok(pagedResult);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Categoría con ID {id} no encontrada.");
            }
            return Ok(category); // Devuelve la entidad completa
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo Admin puede crear
        public async Task<ActionResult<Category>> PostCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // (Opcional: Validar si ya existe una categoría con el mismo nombre)

            await _unitOfWork.CategoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInfo($"Categoría '{category.CategoryName}' (ID: {category.CategoryID}) creada.");
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID }, category);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admin puede modificar
        public async Task<IActionResult> PutCategory(int id, [FromBody] Category categoryUpdate)
        {
            if (id != categoryUpdate.CategoryID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Categoría con ID {id} no encontrada.");
            }

            category.CategoryName = categoryUpdate.CategoryName;
            category.Description = categoryUpdate.Description;
            // No actualizamos Picture aquí, requeriría manejo especial

            _unitOfWork.CategoryRepository.Update(category);

            try
            {
                await _unitOfWork.SaveChangesAsync();
                 _logger.LogInfo($"Categoría ID {id} actualizada.");
            }
            catch (DbUpdateConcurrencyException)
            {
                 if (await _unitOfWork.CategoryRepository.GetByIdAsync(id) == null) return NotFound();
                 else return Conflict("La categoría fue modificada por otro usuario.");
            }

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admin puede eliminar
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Categoría con ID {id} no encontrada.");
            }

            // (Opcional: Validar si la categoría está en uso por algún producto)
            // var isUsed = await _context.Products.AnyAsync(p => p.CategoryID == id);
            // if (isUsed) return BadRequest("No se puede eliminar una categoría que está en uso.");

            _unitOfWork.CategoryRepository.Delete(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInfo($"Categoría ID {id} eliminada.");
            return NoContent();
        }
    }
}