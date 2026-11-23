using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Optional create-time pending defaults. Medical / CV are optional and are not auto-pending.
/// Residence addresses are case-owned — not Person intake.
/// </summary>
public static class PersonIntakeDeferredRecords
{
    public static void EnsureCreateTimePending(Person person, PersonIntakeWizardState state)
    {
        // Medical and travel are not wizard steps. Drop leftover Not-received marks so a wizard
        // save does not keep the person incomplete for records entered from their own navigation.
        state?.PendingKeys.Remove(PersonPendingTargets.MedicalRecord);
        state?.PendingKeys.Remove(PersonPendingTargets.TravelHistory);
        state?.RecordNotes.Remove(PersonPendingTargets.MedicalRecord);
        state?.RecordNotes.Remove(PersonPendingTargets.TravelHistory);
        // Salary is optional. A leftover Not-received mark must not hide the step or keep the person incomplete.
        state?.PendingKeys.Remove(PersonPendingTargets.EmployeeSalary);
        state?.RecordNotes.Remove(PersonPendingTargets.EmployeeSalary);
    }
}