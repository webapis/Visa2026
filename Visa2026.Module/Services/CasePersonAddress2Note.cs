using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Upserts case↔person Address 2 free-text notes (People &amp; links only).
/// </summary>
public static class CasePersonAddress2Note
{
    public static Dictionary<Guid, string> LoadForApplication(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application)
    {
        var map = new Dictionary<Guid, string>();
        if (objectSpace == null || application == null || application.ID == Guid.Empty)
            return map;

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonAddress2Note>()
            .Where(row => row.ApplicationProfileInstanceId == application.ID)
            .Select(row => new { row.PersonId, row.Address2Text })
            .ToList();
        foreach (var row in rows)
        {
            if (row.PersonId == Guid.Empty)
                continue;
            var value = row.Address2Text?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value == "—")
                continue;
            map[row.PersonId] = value;
        }

        return map;
    }

    public static string? GetText(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return null;

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        var row = Find(objectSpace, application, applicationId, personId);
        var value = row?.Address2Text?.Trim();
        return string.IsNullOrWhiteSpace(value) || value == "—" ? null : value;
    }

    public static bool Upsert(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string? text)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return false;
        if (application.ID == Guid.Empty)
            return false;

        var trimmed = Normalize(text);
        var existing = Find(objectSpace, application, application.ID, person.ID);
        if (trimmed == null)
        {
            if (existing == null || objectSpace.IsDeletedObject(existing))
                return false;
            if (!objectSpace.IsNewObject(existing))
                objectSpace.ReloadObject(existing);
            if (objectSpace.IsDeletedObject(existing))
                return false;
            application.PersonAddress2Notes?.Remove(existing);
            objectSpace.Delete(existing);
            return true;
        }

        if (existing == null)
        {
            existing = objectSpace.CreateObject<ApplicationProfileInstancePersonAddress2Note>();
            existing.ApplicationProfileInstance = application;
            existing.ApplicationProfileInstanceId = application.ID;
            existing.Person = person;
            existing.PersonId = person.ID;
            application.PersonAddress2Notes?.Add(existing);
        }

        if (string.Equals(existing.Address2Text?.Trim(), trimmed, StringComparison.Ordinal))
            return false;

        existing.Address2Text = trimmed;
        return true;
    }

    public static void DeleteForPersonOnCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return;

        foreach (var row in EnumerateForPerson(objectSpace, applicationId, personId).ToList())
            objectSpace.Delete(row);
    }

    private static string? Normalize(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || trimmed == "—" ? null : trimmed;
    }

    private static ApplicationProfileInstancePersonAddress2Note? Find(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Guid applicationId,
        Guid personId)
    {
        if (application?.PersonAddress2Notes != null)
        {
            var inMemory = application.PersonAddress2Notes
                .FirstOrDefault(row =>
                    row != null
                    && !objectSpace.IsDeletedObject(row)
                    && row.PersonId == personId);
            if (inMemory != null)
                return inMemory;
        }

        return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonAddress2Note>()
            .FirstOrDefault(row =>
                row.ApplicationProfileInstanceId == applicationId
                && row.PersonId == personId);
    }

    private static IEnumerable<ApplicationProfileInstancePersonAddress2Note> EnumerateForPerson(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonAddress2Note>()
            .Where(row => row.ApplicationProfileInstanceId == applicationId && row.PersonId == personId);
}