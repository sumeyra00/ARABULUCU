using System.Threading.Tasks;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class ContactSubmissionServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsTrimmedContactSubmission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new ContactSubmissionService(dbContext);
        await service.CreateAsync(new ContactSubmissionRequest(
            "  Ayşe Yılmaz ",
            " ayse@example.com ",
            " Destek ",
            "  Yardım almak istiyorum.  ",
            "user-1"));

        var submission = await dbContext.ContactSubmissions.SingleAsync();
        Assert.Equal("Ayşe Yılmaz", submission.Name);
        Assert.Equal("ayse@example.com", submission.Email);
        Assert.Equal("Destek", submission.Subject);
        Assert.Equal("Yardım almak istiyorum.", submission.Message);
        Assert.Equal("user-1", submission.UserId);
    }
}
