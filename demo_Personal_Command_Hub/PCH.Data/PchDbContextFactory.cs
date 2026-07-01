using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PCH.Data;

/// <summary>
/// Design-time factory used by the EF Core tools (e.g. <c>dotnet ef migrations add</c>)
/// to construct a <see cref="PchDbContext"/> without booting the full host.
/// </summary>
public class PchDbContextFactory : IDesignTimeDbContextFactory<PchDbContext>
{
    /// <inheritdoc />
    public PchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PchDbContext>()
            .UseSqlite("Data Source=pch.db")
            .Options;

        return new PchDbContext(options);
    }
}
