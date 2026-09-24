using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum ApplicationProfileInstanceExclusionAddresseeKind
{
    Ministry = 0,
    MigrationService = 1,
    Other = 2,
}

/// <summary>
/// Seretmezlik letter: asks the current holder (ministry leg or Migration Service) to stop
/// processing some roster people of a submitted case. Saving excludes the listed people; not reversible.
/// See <c>docs/prototypes/application-profile-instance-seretmezlik-README.md</c>.
/// </summary>
[Table("ApplicationProfileInstanceExclusions")]
[NavigationItem(false)]
[XafDisplayName("Seretmezlik")]
[DefaultProperty(nameof(LetterNumber))]
public class ApplicationProfileInstanceExclusion : BaseObject
{
    public ApplicationProfileInstanceExclusion()
    {
        People = new ObservableCollection<ApplicationProfileInstanceExclusionPerson>();
    }

    [Browsable(false)]
    public virtual Guid ApplicationProfileInstanceId { get; set; }

    [Browsable(false)]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; } = null!;

    /// <summary>Numbering prefix at allocation (shared with <see cref="ApplicationProfileInstance.AppNumberPrefix"/>).</summary>
    [MaxLength(50)]
    [Browsable(false)]
    public virtual string? AppNumberPrefix { get; set; }

    [Browsable(false)]
    public virtual int Year { get; set; }

    [Browsable(false)]
    public virtual int Month { get; set; }

    /// <summary>Sequence part of the outgoing letter number (same counter as application numbers).</summary>
    [MaxLength(50)]
    [Browsable(false)]
    public virtual string? SequenceNumber { get; set; }

    [RuleRequiredField]
    [MaxLength(100)]
    [XafDisplayName("Letter №")]
    public virtual string LetterNumber { get; set; } = string.Empty;

    [RuleRequiredField]
    [XafDisplayName("Letter date")]
    public virtual DateTime LetterDate { get; set; }

    [XafDisplayName("Addressee kind")]
    public virtual ApplicationProfileInstanceExclusionAddresseeKind AddresseeKind { get; set; }

    /// <summary>Ministry leg (1…N) when <see cref="AddresseeKind"/> is Ministry.</summary>
    [Browsable(false)]
    public virtual int? AddresseeLeg { get; set; }

    /// <summary>Recipient block as printed (dative, e.g. <c>Türkmenistanyň Döwlet migrasiýa gullugyna</c>).</summary>
    [RuleRequiredField]
    [MaxLength(500)]
    [XafDisplayName("Addressee")]
    public virtual string AddresseeName { get; set; } = string.Empty;

    [MaxLength(300)]
    [XafDisplayName("Salutation")]
    public virtual string? Salutation { get; set; }

    [MaxLength(300)]
    [XafDisplayName("Refers to (ministry)")]
    public virtual string? ReferenceMinistryName { get; set; }

    [XafDisplayName("Ministry letter date")]
    public virtual DateTime? ReferenceLetterDate { get; set; }

    [MaxLength(100)]
    [XafDisplayName("Ministry letter №")]
    public virtual string? ReferenceLetterNumber { get; set; }

    [XafDisplayName("Original roster (people)")]
    public virtual int OriginalRosterCount { get; set; }

    [MaxLength(700)]
    [XafDisplayName("Subject of the original application")]
    public virtual string? Subject { get; set; }

    [Browsable(false)]
    public virtual DateTime? CreatedOnUtc { get; set; }

    [MaxLength(255)]
    [Browsable(false)]
    public virtual string? CreatedByUserName { get; set; }

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileInstanceExclusionPerson.Exclusion))]
    [XafDisplayName("Excluded people")]
    public virtual IList<ApplicationProfileInstanceExclusionPerson> People { get; set; }
}
