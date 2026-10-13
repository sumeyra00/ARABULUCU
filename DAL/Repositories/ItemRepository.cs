using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Repositories
{
    // DAL repository implementasyonu: EF Core üzerinden veri erişimi sağlar.
    public class ItemRepository : IItemRepository
    {
        private readonly ApplicationDbContext _db;

        public ItemRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(LostItem entity)
        {
            await _db.LostItems.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(LostItem entity)
        {
            _db.LostItems.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<LostItem>> GetAllAsync()
        {
            return await _db.LostItems.AsNoTracking().ToListAsync();
        }

        public async Task<LostItem?> GetByIdAsync(int id)
        {
            return await _db.LostItems.FindAsync(id);
        }

        public async Task UpdateAsync(LostItem entity)
        {
            _db.LostItems.Update(entity);
            await _db.SaveChangesAsync();
        }
    }
}
