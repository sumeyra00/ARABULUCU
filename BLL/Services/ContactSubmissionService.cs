using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public sealed class ContactSubmissionService : IContactSubmissionService
{
    private readonly ApplicationDbContext _dbContext;

    public ContactSubmissionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(ContactSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.ContactSubmissions.Add(new ContactSubmission
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
