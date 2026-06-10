using System.Xml.Linq;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public sealed class DatabaseDataProtectionXmlRepository(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseDataProtectionXmlRepository> logger) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var keyRows = context.DataProtectionKeys
            .AsNoTracking()
            .OrderBy(key => key.Id)
            .Select(key => new { key.Id, key.FriendlyName, key.Xml })
            .ToList();

        var elements = new List<XElement>(keyRows.Count);
        foreach (var key in keyRows)
        {
            try
            {
                elements.Add(XElement.Parse(key.Xml));
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Stored Data Protection key XML could not be parsed. KeyId: {DataProtectionKeyId}; FriendlyName: {FriendlyName}",
                    key.Id,
                    key.FriendlyName);
            }
        }

        logger.LogDebug("Loaded {DataProtectionKeyCount} persisted Data Protection keys.", elements.Count);
        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);

        var keyName = string.IsNullOrWhiteSpace(friendlyName)
            ? $"key-{Guid.NewGuid():N}"
            : friendlyName.Trim();
        var now = DateTime.UtcNow;
        var xml = element.ToString(SaveOptions.DisableFormatting);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingKey = context.DataProtectionKeys.SingleOrDefault(key => key.FriendlyName == keyName);

        if (existingKey is null)
        {
            context.DataProtectionKeys.Add(new DataProtectionKey
            {
                FriendlyName = keyName,
                Xml = xml,
                CreatedAt = now
            });
        }
        else
        {
            existingKey.Xml = xml;
            existingKey.LastModifiedAt = now;
        }

        context.SaveChanges();
        logger.LogInformation("Stored Data Protection key {FriendlyName} in PostgreSQL.", keyName);
    }
}
