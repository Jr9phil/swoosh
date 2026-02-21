using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swoosh.Api.Data;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

/// Background service that periodically re-encrypts tasks when the active encryption key version changes.
/// This ensures that all data eventually migrates to the latest key.
public class ReencryptionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ReencryptionService> _logger;

    public ReencryptionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ReencryptionService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }
    
    /// Retrieves the encryption salt for a specific user from the database.
    private async Task<byte[]> GetUserSalt(Guid userId, AppDbContext db)
    {
        try
        {
            return await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.EncryptionSalt)
                .SingleAsync();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new Exception("Failed while retrieving user salt", e);
        }
    }

    /// Main execution loop for the background service.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task re-encryption service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReencryptBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during task re-encryption");
            }

            // Run periodically (e.g., every 10 minutes)
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    /// Processes a batch of tasks that are using an outdated encryption key version.
    private async Task ReencryptBatchAsync(CancellationToken ct)
    {
        var activeVersion = int.Parse(_config["Encryption:ActiveKeyVersion"]!);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crypto = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        // Small batch to avoid locks
        var tasks = await db.Tasks
            .Where(t => t.KeyVersion < activeVersion)
            .OrderBy(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (!tasks.Any())
            return;
        

        foreach (var task in tasks)
        {
            var salt = await GetUserSalt(task.UserId, db);
            // Decrypt with old key
            var title = crypto.Decrypt(task.EncryptedTitle, task.UserId, task.KeyVersion, salt);
            var notes = task.EncryptedNotes == null
                ? null
                : crypto.Decrypt(task.EncryptedNotes, task.UserId, task.KeyVersion,salt);

            // Encrypt with new key
            var (newTitle, newVersion) = crypto.Encrypt(title, task.UserId,salt);
            task.EncryptedTitle = newTitle;
            task.KeyVersion = newVersion;

            if (notes != null)
                task.EncryptedNotes = crypto.Encrypt(notes, task.UserId, salt).Ciphertext;
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Re-encrypted {Count} tasks to key version {Version}",
            tasks.Count,
            activeVersion
        );
    }
}
