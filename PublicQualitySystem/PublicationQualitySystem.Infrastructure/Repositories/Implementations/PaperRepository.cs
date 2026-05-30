using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PaperRepository(ApplicationDbContext db) : IPaperRepository
{
    public async Task<Paper> CreateAsync(Paper paper)
    {
        try
        {
            db.Papers.Add(paper);
            await db.SaveChangesAsync();
            return paper;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating paper: {ex.Message}");
            throw;
        }
    }

    public Task<Paper?> FindByIdWithAccessDataAsync(long id) =>
        db.Papers
            .Include(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(p => p.Id == id && !p.Deleted);
}
