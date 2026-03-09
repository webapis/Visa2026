using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Visa2026.Module.Services;
using DevExpress.Xpo;

namespace Visa2026.Module.BusinessObjects
{
    public enum PdfMappingMode
    {
        Property,
        Expression,
        Constant
    }

    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(Description))]
    public class PdfFormMapping : BaseObject
    {
        [RuleRequiredField]
        [ToolTip("The XFA field name in the PDF template (e.g., topmostSubform[0].Page2[0]._33[0])")]
        public virtual string PdfFieldKey { get; set; }

        [RuleRequiredField]
        public virtual string Description { get; set; }

        [ImmediatePostData]
        public virtual PdfMappingMode MappingMode { get; set; }

        [Appearance("PropertyPathVisible", Visibility = ViewItemVisibility.Hide, Criteria = "MappingMode != 'Property'")]
        [ToolTip("The property path relative to ApplicationItem (e.g., Person.LastName, Application.VisaType.PdfForm_Code)")]
        public virtual string PropertyPath { get; set; }

        [Appearance("ExpressionVisible", Visibility = ViewItemVisibility.Hide, Criteria = "MappingMode != 'Expression'")]
        [CriteriaOptions(nameof(TargetType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [Size(SizeAttribute.Unlimited)]
        [ToolTip("An expression to calculate the value. E.g., Iif(Person.Company != null, Person.Company.Name, 'N/A')")]
        public virtual string Expression { get; set; }

        [Appearance("ConstantValueVisible", Visibility = ViewItemVisibility.Hide, Criteria = "MappingMode != 'Constant'")]
        [ToolTip("A fixed value to assign to the PDF field.")]
        public virtual string ConstantValue { get; set; }

        [Browsable(false)]
        public virtual string ConverterTypeName { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableConverterNames))]
        [System.ComponentModel.DisplayName("Converter Type")]
        public virtual string Converter
        {
            get
            {
                if (string.IsNullOrEmpty(ConverterTypeName)) return null;
                return Type.GetType(ConverterTypeName)?.Name;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ConverterTypeName = null;
                }
                else
                {
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(s => s.GetTypes())
                        .FirstOrDefault(t => t.Name == value && typeof(IValueConverter).IsAssignableFrom(t));
                    ConverterTypeName = type?.AssemblyQualifiedName;
                }
            }
        }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<string> AvailableConverterNames => AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IValueConverter).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                    .Select(t => t.Name)
                    .OrderBy(n => n)
                    .ToList();

        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty("PdfFormMapping_ExpressionValid", DefaultContexts.Save, "Invalid Expression syntax.")]
        public bool IsExpressionValid
        {
            get
            {
                if (MappingMode != PdfMappingMode.Expression || string.IsNullOrEmpty(Expression)) return true;
                try
                {
                    CriteriaOperator.Parse(Expression);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [NotMapped]
        [Browsable(false)]
        public Type TargetType => typeof(ApplicationItem);
    }
}