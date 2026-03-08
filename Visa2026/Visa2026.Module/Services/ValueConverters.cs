using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services
{
    public interface IValueConverter
    {
        object Convert(object value, IObjectSpace objectSpace);
    }

    public class GenderValueConverter : IValueConverter
    {
        public object Convert(object value, IObjectSpace objectSpace)
        {
            // For Gender, we treat the display value as the raw value if it exists in the mapping
            // or if it matches the raw value directly.
            return PdfFormConstants.GetValue("Gender", value?.ToString(), objectSpace);
        }
    }

    public class MaritalStatusValueConverter : IValueConverter
    {
        public object Convert(object value, IObjectSpace objectSpace)
        {
            return PdfFormConstants.GetValue("MaritalStatus", value?.ToString(), objectSpace);
        }
    }

    public class PassportTypeValueConverter : IValueConverter
    {
        public object Convert(object value, IObjectSpace objectSpace)
        {
            return PdfFormConstants.GetValue("PassportType", value?.ToString(), objectSpace);
        }
    }
    
    public class UrgencyValueConverter : IValueConverter
    {
        public object Convert(object value, IObjectSpace objectSpace)
        {
            return PdfFormConstants.GetValue("Urgency", value?.ToString(), objectSpace);
        }
    }
}