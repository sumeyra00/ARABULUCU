using System;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class VerificationServiceTests
{
    [Fact]
    public async Task GetVerificationQuestionsAsync_IncludesLegacyStandardFieldsWithoutFlag()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();

        var owner = new UserProfile { Id = "owner", DisplayName = "Owner", Email = "owner@example.com" };
        var category = new Category { Id = 201, Name = "Telefon" };
        var item = new LostItem { User = owner, Category = category, Title = "Telefon", Description = "", Location = "Kampüs" };
        db.UserProfiles.Add(owner);
        db.Categories.Add(category);
        db.LostItems.Add(item);
        await db.SaveChangesAsync();
        db.LostItemFeatures.Add(new LostItemFeature
        {
            LostItemId = item.Id, Key = "SecurityAnswer", Value = "Arka etiket", IsHidden = true
        });
        await db.SaveChangesAsync();

        var questions = await new LostItemListingService(db, new ItemRepository(db), new LostItemSimilarityService())
            .GetVerificationQuestionsAsync(item.Id);

        var question = Assert.Single(questions);
        Assert.Equal("Gizli doğrulama detayı", question.Prompt);
    }

    [Fact]
    public async Task VerifyAsync_RequiresThreeCorrectAnswersAndFiftyPoints()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();

        var owner = new UserProfile { Id = "owner", DisplayName = "Owner", Email = "owner@example.com" };
        var category = new Category { Id = 200, Name = "Wallet" };
        var item = new LostItem
        {
            User = owner,
            Category = category,
            Title = "Wallet",
            Description = "Private description",
            Location = "Private location"
        };
        db.UserProfiles.Add(owner);
        db.Categories.Add(category);
        db.LostItems.Add(item);
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<LostItemFeature>();
        foreach (var (prompt, answer, weight) in new[]
        {
            ("Renk", "Mavi", 25),
            ("Marka", "Acme", 25),
            ("Özel soru", "Kart", 30)
        })
        {
            var feature = new LostItemFeature
            {
                LostItemId = item.Id,
                Key = prompt,
                Value = prompt == "Özel soru" ? "Özel cevap" : answer,
                IsHidden = true,
                IsVerificationQuestion = true,
                Weight = weight
            };
            feature.AnswerHash = hasher.HashPassword(feature, answer.Trim().ToUpperInvariant());
            db.LostItemFeatures.Add(feature);
        }
        await db.SaveChangesAsync();

        var service = new LostItemListingService(db, new ItemRepository(db), new LostItemSimilarityService());
        var questions = await service.GetVerificationQuestionsAsync(item.Id);
        var failed = await service.VerifyAsync(item.Id, "claimer", questions.Take(2)
            .Select(question => new VerificationAnswerDto { QuestionId = question.Id, Answer = question.Prompt == "Renk" ? "Mavi" : "Acme" })
            .ToArray());

        Assert.False(failed.IsSuccessful);
        Assert.Equal("Girdiğiniz bilgiler ilanla eşleşmedi.", failed.Error);

        var succeeded = await service.VerifyAsync(item.Id, "claimer", questions
            .Select(question => new VerificationAnswerDto
            {
                QuestionId = question.Id,
                Answer = question.Prompt == "Renk" ? "Mavi" : question.Prompt == "Marka" ? "Acme" : "Kart"
            })
            .ToArray());

        Assert.True(succeeded.IsSuccessful);
        Assert.Single(db.VerificationMatches);

        var protectedDetails = await service.GetProtectedDetailsAsync(item.Id, "claimer");
        Assert.NotNull(protectedDetails);
        Assert.Contains(protectedDetails.Features, feature => feature.Key == "Renk");
        Assert.DoesNotContain(protectedDetails.Features, feature => feature.Key == "Özel soru");
    }

    [Fact]
    public async Task FindMatchesAsync_RequiresLocationDateThreeAnswersAndFiftyPoints()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();

        var owner = new UserProfile { Id = "owner", DisplayName = "Owner", Email = "owner@example.com" };
        var category = new Category { Id = 202, Name = "Telefon" };
        var item = new LostItem
        {
            User = owner,
            Category = category,
            Title = "Telefon",
            Description = "Telefon",
            Location = "Merkez Kütüphane",
            EventDate = new DateTime(2026, 11, 5)
        };
        db.UserProfiles.Add(owner);
        db.Categories.Add(category);
        db.LostItems.Add(item);
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<LostItemFeature>();
        foreach (var (key, answer, weight) in new[]
        {
            ("Brand", "Acme", 20),
            ("Model", "X1", 20),
            ("Color", "Mavi", 20)
        })
        {
            var feature = new LostItemFeature
            {
                LostItemId = item.Id,
                Key = key,
                Value = answer,
                IsHidden = true,
                IsVerificationQuestion = true,
                Weight = weight
            };
            feature.AnswerHash = hasher.HashPassword(feature, answer.ToUpperInvariant());
            db.LostItemFeatures.Add(feature);
        }
        await db.SaveChangesAsync();

        var service = new LostItemListingService(db, new ItemRepository(db), new LostItemSimilarityService());
        var questions = await service.GetVerificationQuestionsAsync(item.Id);
        var result = await service.FindMatchesAsync(new MatchingCriteria
        {
            Location = "kütüphane",
            EventDate = new DateTime(2026, 11, 5),
            Answers = questions.Select(question => new VerificationAnswerDto
            {
                QuestionId = question.Id,
                Answer = question.Prompt switch
                {
                    "Marka nedir?" => "Acme",
                    "Model nedir?" => "X1",
                    _ => "Mavi"
                }
            }).ToArray()
        });

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Items);

        var wrongDate = await service.FindMatchesAsync(new MatchingCriteria
        {
            Location = "kütüphane",
            EventDate = new DateTime(2026, 11, 6),
            Answers = []
        });

        Assert.False(wrongDate.IsSuccessful);
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
}
