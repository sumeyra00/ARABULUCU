using System.Collections.Generic;
using System.Threading.Tasks;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Repositories
{
    /// <summary>
    /// DAL içinde item odaklı repository arayüzü.
    /// İş katmanı (BLL) bu arayüzü kullanarak veri erişimini soyutlar.
    /// </summary>
    public interface IItemRepository
    {
        /// <summary>Id'ye göre ilan getirir (null dönebilir)</summary>
        Task<LostItem?> GetByIdAsync(int id);
        /// <summary>Tüm ilanları getirir</summary>
        Task<IEnumerable<LostItem>> GetAllAsync();
        /// <summary>Yeni ilan ekler</summary>
        Task AddAsync(LostItem entity);
        /// <summary>İlanı günceller</summary>
        Task UpdateAsync(LostItem entity);
        /// <summary>İlanı siler</summary>
        Task DeleteAsync(LostItem entity);
    }
}
