using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Non-persistent dialog for Mark incomplete on <see cref="Person"/>.</summary>
[DomainComponent]
[DefaultClassOptions]
[XafDisplayName("Mark person incomplete")]
public class PersonIncompleteMarkOptions : NonPersistentBaseObject
{
    [XafDisplayName("Personal data")]
    public virtual bool MissingPersonalData { get; set; }

    [XafDisplayName("Passport")]
    public virtual bool MissingPassport { get; set; }

    [XafDisplayName("CV")]
    public virtual bool MissingCv { get; set; }

    [XafDisplayName("Photo")]
    public virtual bool MissingPhoto { get; set; }

    [XafDisplayName("Education")]
    public virtual bool MissingEducation { get; set; }

    [XafDisplayName("Medical")]
    public virtual bool MissingMedical { get; set; }

    [XafDisplayName("Address")]
    public virtual bool MissingAddress { get; set; }

    [XafDisplayName("Family docs")]
    public virtual bool MissingFamilyDocs { get; set; }

    [XafDisplayName("Other")]
    public virtual bool MissingOther { get; set; }

    [XafDisplayName("Notes")]
    [ToolTip("Describe what is missing.")]
    [FieldSize(FieldSizeAttribute.Unlimited)]
    [RuleRequiredField]
    public virtual string Notes { get; set; }

    [Browsable(false)]
    [RuleFromBoolProperty(
        "PersonIncompleteMark_AtLeastOneArea",
        DefaultContexts.Save,
        "Select at least one missing-data area.")]
    public bool HasAtLeastOneMissingArea =>
        MissingPersonalData || MissingPassport || MissingCv || MissingPhoto
        || MissingEducation || MissingMedical || MissingAddress || MissingFamilyDocs || MissingOther;

    public void ApplyTo(Person person, string markedBy)
    {
        person.IsDataIncomplete = true;
        person.IncompleteMissingPersonalData = MissingPersonalData;
        person.IncompleteMissingPassport = MissingPassport;
        person.IncompleteMissingCv = MissingCv;
        person.IncompleteMissingPhoto = MissingPhoto;
        person.IncompleteMissingEducation = MissingEducation;
        person.IncompleteMissingMedical = MissingMedical;
        person.IncompleteMissingAddress = MissingAddress;
        person.IncompleteMissingFamilyDocs = MissingFamilyDocs;
        person.IncompleteMissingOther = MissingOther;
        person.IncompleteNotes = Notes?.Trim();
        person.IncompleteMarkedOn = System.DateTime.Now;
        person.IncompleteMarkedBy = markedBy;
    }

    public void LoadFrom(Person person)
    {
        MissingPersonalData = person.IncompleteMissingPersonalData;
        MissingPassport = person.IncompleteMissingPassport;
        MissingCv = person.IncompleteMissingCv;
        MissingPhoto = person.IncompleteMissingPhoto;
        MissingEducation = person.IncompleteMissingEducation;
        MissingMedical = person.IncompleteMissingMedical;
        MissingAddress = person.IncompleteMissingAddress;
        MissingFamilyDocs = person.IncompleteMissingFamilyDocs;
        MissingOther = person.IncompleteMissingOther;
        Notes = person.IncompleteNotes;
    }
}