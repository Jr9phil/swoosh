using Microsoft.EntityFrameworkCore;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

/// Background service that spawns real TaskItems from active recurring tasks.
/// Runs on startup and then every hour. For each active recurring task, it checks
/// every date from the day after the last spawn through today, and creates a TaskItem
/// for each occurrence. The spawned task's deadline is the recurrence time on that day,
/// or 23:59 if no time is configured.
public class RecurringTaskSpawnService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringTaskSpawnService> _logger;

    public RecurringTaskSpawnService(IServiceScopeFactory scopeFactory, ILogger<RecurringTaskSpawnService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurring task spawn service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SpawnDueTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recurring task spawn");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    /// Returns true if the recurrence with the given type/interval/start lands on `day`.
    private static bool OccursOnDay(string type, int interval, DateOnly start, DateOnly day)
    {
        if (day < start) return false;
        if (day == start) return true;

        return type switch
        {
            "day"   => (day.DayNumber - start.DayNumber) % interval == 0,
            "week"  => (day.DayNumber - start.DayNumber) % (7 * interval) == 0,
            "month" => day.Day == start.Day &&
                       ((day.Year - start.Year) * 12 + (day.Month - start.Month)) % interval == 0,
            "year"  => day.Month == start.Month && day.Day == start.Day &&
                       (day.Year - start.Year) % interval == 0,
            _       => false
        };
    }

    private async Task SpawnDueTasksAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crypto = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rows = await db.RecurringTasks
            .Join(db.Users, r => r.UserId, u => u.Id, (r, u) => new { Task = r, u.EncryptionSalt })
            .ToListAsync(ct);

        int spawned = 0;

        foreach (var row in rows)
        {
            var r      = row.Task;
            var salt   = row.EncryptionSalt;
            var userId = r.UserId;

            try
            {
                var isActive = crypto.DecryptBool(r.EncryptedIsActive, userId, r.KeyVersion, salt);
                if (!isActive) continue;

                var type     = crypto.Decrypt(r.EncryptedRecurrenceType, userId, r.KeyVersion, salt);
                var interval = r.EncryptedRecurrenceInterval != null
                    ? (crypto.DecryptNullableInt(r.EncryptedRecurrenceInterval, userId, r.KeyVersion, salt) ?? 1)
                    : 1;
                var dateStr  = r.EncryptedDate != null
                    ? crypto.DecryptNullableString(r.EncryptedDate, userId, r.KeyVersion, salt)
                    : null;
                var timeStr  = r.EncryptedTime != null
                    ? crypto.DecryptNullableString(r.EncryptedTime, userId, r.KeyVersion, salt)
                    : null;
                var lastSpawnedStr = r.EncryptedLastSpawnedDate != null
                    ? crypto.DecryptNullableString(r.EncryptedLastSpawnedDate, userId, r.KeyVersion, salt)
                    : null;

                var start       = dateStr != null && DateOnly.TryParse(dateStr, out var d) ? d : today;
                var lastSpawned = lastSpawnedStr != null && DateOnly.TryParse(lastSpawnedStr, out var ls) ? (DateOnly?)ls : null;

                var checkFrom = lastSpawned.HasValue ? lastSpawned.Value.AddDays(1) : start;
                var checkTo   = today;

                if (checkFrom > checkTo)
                {
                    // Already up-to-date; update LastSpawnedDate in case it was never set
                    if (lastSpawned == null)
                        r.EncryptedLastSpawnedDate = crypto.EncryptNullableString(today.ToString("yyyy-MM-dd"), userId, salt).Ciphertext;
                    continue;
                }

                // Decrypt task content once — same for all spawned instances
                var title    = crypto.Decrypt(r.EncryptedTitle, userId, r.KeyVersion, salt);
                var notes    = crypto.DecryptNullableString(r.EncryptedNotes, userId, r.KeyVersion, salt);
                var priority = r.EncryptedPriority != null ? crypto.DecryptInt(r.EncryptedPriority, userId, r.KeyVersion, salt) : 0;
                var pinned   = r.EncryptedPinned != null && crypto.DecryptBool(r.EncryptedPinned, userId, r.KeyVersion, salt);
                var icon     = r.EncryptedIcon != null ? crypto.DecryptNullableInt(r.EncryptedIcon, userId, r.KeyVersion, salt) : null;

                for (var day = checkFrom; day <= checkTo; day = day.AddDays(1))
                {
                    if (!OccursOnDay(type, interval, start, day)) continue;

                    // Deadline: recurrence time if set, otherwise 23:59 on that day.
                    // DateTimeKind.Unspecified matches how the frontend sends deadlines (no timezone suffix),
                    // so the client treats the time as local rather than converting from UTC.
                    DateTime deadline;
                    if (timeStr != null && TimeOnly.TryParse(timeStr, out var t))
                        deadline = new DateTime(day.Year, day.Month, day.Day, t.Hour, t.Minute, 0, DateTimeKind.Unspecified);
                    else
                        deadline = new DateTime(day.Year, day.Month, day.Day, 23, 59, 0, DateTimeKind.Unspecified);

                    var (encTitle, keyVer) = crypto.Encrypt(title, userId, salt);
                    var now = DateTime.UtcNow;

                    db.Tasks.Add(new TaskItem
                    {
                        Id                  = Guid.NewGuid(),
                        UserId              = userId,
                        EncryptedTitle      = encTitle,
                        EncryptedNotes      = crypto.EncryptNullableString(notes, userId, salt).Ciphertext,
                        EncryptedDeadline   = crypto.EncryptNullableDateTime(deadline, userId, salt).Ciphertext,
                        EncryptedCompletedAt = crypto.EncryptNullableDateTime(null, userId, salt).Ciphertext,
                        EncryptedPinned     = crypto.EncryptBool(pinned, userId, salt).Ciphertext,
                        EncryptedPriority   = crypto.EncryptInt(priority, userId, salt).Ciphertext,
                        EncryptedRating     = crypto.EncryptInt(0, userId, salt).Ciphertext,
                        EncryptedIcon       = icon.HasValue ? crypto.EncryptNullableInt(icon.Value, userId, salt).Ciphertext : null,
                        KeyVersion          = keyVer,
                        CreatedAt           = now,
                        Modified            = now,
                    });

                    spawned++;
                }

                r.EncryptedLastSpawnedDate = crypto.EncryptNullableString(today.ToString("yyyy-MM-dd"), userId, salt).Ciphertext;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process recurring task {Id}", r.Id);
            }
        }

        await db.SaveChangesAsync(ct);

        if (spawned > 0)
            _logger.LogInformation("Spawned {Count} task(s) from recurring tasks", spawned);
    }
}
