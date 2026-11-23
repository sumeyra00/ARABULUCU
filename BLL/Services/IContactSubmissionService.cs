using ARABULUCU.BLL.DTOs;

namespace ARABULUCU.BLL.Services;

public interface IContactSubmissionService
{
    Task CreateAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default);
}
