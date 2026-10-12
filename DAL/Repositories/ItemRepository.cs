using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Repositories
{
    /// <summary>
    /// DAL repository implementasyonu: EF Core üzerinden veri erişimi sağlar.
    /// Bu sınıf IItemRepository arayüzünü uygular ve ApplicationDbContext ile çalışır.
    /// </summary>
    public class ItemRepository : IItemRepository
    {
        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Constructor: DbContext bağımlılığı (DI) alınır.
        /// </summary>
        public ItemRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Yeni bir LostItem ekler ve değişiklikleri kaydeder.
        /// </summary>
        public async Task AddAsync(LostItem entity)
        {
            await _db.LostItems.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Verilen LostItem kaydını siler ve değişiklikleri kaydeder.
        /// </summary>
        public async Task DeleteAsync(LostItem entity)
        {
            _db.LostItems.Remove(entity);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Tüm ilanları getirir (AsNoTracking ile okunur).
        /// </summary>
        public async Task<IEnumerable<LostItem>> GetAllAsync()
        {
            return await _db.LostItems.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Id'ye göre ilan getirir (null dönebilir).
        /// </summary>
        public async Task<LostItem?> GetByIdAsync(int id)
        {
            return await _db.LostItems.FindAsync(id);
        }

        /// <summary>
        /// Mevcut ilanı günceller ve değişiklikleri kaydeder.
        /// </summary>
        public async Task UpdateAsync(LostItem entity)
        {
            _db.LostItems.Update(entity);
            await _db.SaveChangesAsync();
        }
    }
}
