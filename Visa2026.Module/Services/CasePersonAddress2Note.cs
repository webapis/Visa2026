using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Upserts case↔person Address 1 / Address 2 free-text notes (People &amp; links only).
/// </summary>
public static class CasePersonAddress2Note
{
    public static Dictionary<Guid, string> LoadForApplication(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application) =>
        LoadFieldForApplication(objectSpace, application, address1: false);

    public static Dictionary<Guid, string> LoadAddress1ForApplication(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application) =>
        LoadFieldForApplication(objectSpace, application, address1: true);

    public static string? GetText(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId) =>
        GetFieldText(objectSpace, applicationId, personId, address1: false);

    public static string? GetAddress1Text(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId) =>
        GetFieldText(objectSpace, applicationId, personId, address1: true);

    public static bool Upsert(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string? text) =>
        UpsertField(objectSpace, application, person, text, address1: false);

    public static bool UpsertAddress1(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string? text) =>
        UpsertField(objectSpace, application, person, text, address1: true);

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

    private static Dictionary<Guid, string> LoadFieldForApplication(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        bool address1)
    {
        var map = new Dictionary<Guid, string>();
        if (objectSpace == null || application == null || application.ID == Guid.Empty)
            return map;

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonAddress2Note>()
            .Where(row => row.ApplicationProfileInstanceId == application.ID)
            .Select(row => new { row.PersonId, row.Address1Text, row.Address2Text })
            .ToList();
        foreach (var row in rows)
        {
            if (row.PersonId == Guid.Empty)
                continue;
            var value = (address1 ? row.Address1Text : row.Address2Text)?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value == "—")
                continue;
            map[row.PersonId] = value;
        }

        return map;
    }

    private static string? GetFieldText(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        bool address1)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return null;

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        var row = Find(objectSpace, application, applicationId, personId);
        var value = (address1 ? row?.Address1Text : row?.Address2Text)?.Trim();
        return string.IsNullOrWhiteSpace(value) || value == "—" ? null : value;
    }

    private static bool UpsertField(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string? text,
        bool address1)
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

            if (address1)
                existing.Address1Text = null;
            else
                existing.Address2Text = null;

            if (Normalize(existing.Address1Text) == null && Normalize(existing.Address2Text) == null)
            {
                if (!objectSpace.IsNewObject(existing))
                    objectSpace.ReloadObject(existing);
                if (objectSpace.IsDeletedObject(existing))
                    return false;
                application.PersonAddress2Notes?.Remove(existing);
                objectSpace.Delete(existing);
            }

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

        var current = address1 ? existing.Address1Text : existing.Address2Text;
        if (string.Equals(current?.Trim(), trimmed, StringComparison.Ordinal))
            return false;

        if (address1)
            existing.Address1Text = trimmed;
        else
            existing.Address2Text = trimmed;
        return true;
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