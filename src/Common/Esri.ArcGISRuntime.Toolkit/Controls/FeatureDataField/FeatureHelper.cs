﻿using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    internal static class FeatureHelper
    {
        public static FieldInfo GetFieldInfo(this GdbFeature feature, string fieldName)
        {
            if (feature == null || string.IsNullOrEmpty(fieldName))
                return null;

            return feature.Schema.Fields.FirstOrDefault(x => x.Name == fieldName);
        }

        public static CodedValueDomain GetCodedValueDomain(this FieldInfo fieldInfo)
        {
            return fieldInfo != null ? fieldInfo.Domain as CodedValueDomain : null;
        }

        public static object GetDisplayValue(this FieldInfo fieldInfo, object value)
        {
            // if FieldInfo cannot be found or FieldInfo doesn't contain CodeValueDoman return original value.
            if (fieldInfo == null || !(fieldInfo.Domain is CodedValueDomain)) return value;

            var cvd = (CodedValueDomain)fieldInfo.Domain;

            // Perform coded value lookup.
            return (cvd.CodedValues != null && cvd.CodedValues.ContainsKey(value)) ? cvd.CodedValues[value] : value;      
        }

        public static KeyValuePair<object,string> GetCodedValue(this CodedValueDomain domain, object value, bool nullable = false)
        {
            if (domain == null || domain.CodedValues == null)
                return default (KeyValuePair<object, string>);

            if (value == null && nullable) 
                return new KeyValuePair<object, string>(null,"");

            return domain.CodedValues.FirstOrDefault(x => x.Key != null && x.Key.Equals(value));
        }
    }
}