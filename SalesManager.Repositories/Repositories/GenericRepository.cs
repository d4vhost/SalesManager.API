using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }

        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            // FindAsync puede devolver null si no encuentra la entidad
            // IMPORTANTE: FindAsync SIEMPRE rastrea la entidad (no usa .AsNoTracking())
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            // FindAsync puede devolver null si no encuentra la entidad
            // IMPORTANTE: FindAsync SIEMPRE rastrea la entidad (no usa .AsNoTracking())
            return await _context.Set<T>().FindAsync(id);
        }

        // --- MÉTODO UPDATE CORREGIDO ---
        public void Update(T entity)
        {
            // En lugar de usar _context.Set<T>().Update(entity),
            // que puede causar conflictos si la entidad ya está rastreada,
            // simplemente adjuntamos la entidad y marcamos su estado como Modificado.
            // Esto es más seguro y maneja correctamente las entidades
            // que ya fueron cargadas por GetByIdAsync.
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}