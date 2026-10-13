using System.Collections.Generic;
using System.Threading.Tasks;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Repositories
{
    // DAL içinde item odaklı repository arayüzü. BLL bu arayüzü kullanarak veri erişimi soyutlar.
    public interface IItemRepository
    {
        Task<LostItem?> GetByIdAsync(int id);
        Task<IEnumerable<LostItem>> GetAllAsync();
        Task AddAsync(LostItem entity);
        Task UpdateAsync(LostItem entity);
        Task DeleteAsync(LostItem entity);
    }
}
