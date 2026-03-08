using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services
{
    public interface IValueConverter
    {
        object Convert(object value);
    }

    public class GenderValueConverter : IValueConverter
    {
        public object Convert(object value)
        {
            if (value == null) return null;
            string stringValue = value.ToString();
            if (PdfFormConstants.GenderRawValues.Contains(stringValue))
            {
                return stringValue;
            }
            return null; 
        }
    }

    public class MaritalStatusValueConverter : IValueConverter
    {
        public object Convert(object value)
        {
            if (value == null) return null;
            string stringValue = value.ToString();
            if (PdfFormConstants.MaritalStatusRawValues.TryGetValue(stringValue, out string raw))
            {
                return raw;
            }
            return stringValue; 
        }
    }

    public class PassportTypeValueConverter : IValueConverter
    {
        public object Convert(object value)
        {
            if (value == null) return null;
            string stringValue = value.ToString();
            if (PdfFormConstants.PassportTypeRawValues.TryGetValue(stringValue, out string raw))
            {
                return raw;
            }
            return stringValue;
        }
    }
    
    public class UrgencyValueConverter : IValueConverter
    {
        public object Convert(object value)
        {
            if (value == null) return null;
            string stringValue = value.ToString();
            if (PdfFormConstants.UrgencyRawValues.TryGetValue(stringValue, out string raw))
            {
                return raw;
            }
            return stringValue;
        }
    }
}