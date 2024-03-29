﻿using System.Reflection;
using System.Text.RegularExpressions;
namespace Toolkit.SampleApp.Maui
{
    public class SampleDatasource
    {
        private SampleDatasource()
        {
            var pages = from t in this.GetType().GetTypeInfo().Assembly.ExportedTypes
                        where t.GetTypeInfo().IsSubclassOf(typeof(ContentPage)) && (t.FullName?.Contains(".Maui.Samples.") ?? false)
                        select t;

            Samples = (from p in pages select new Sample(p)).ToArray();
        }

        public IEnumerable<Sample> Samples { get; private set; }

        private static SampleDatasource? m_Current;

        public static SampleDatasource Current
        {
            get
            {
                if (m_Current == null)
                    m_Current = new SampleDatasource();
                return m_Current;
            }
        }
    }
    public class SampleInfoAttribute : Attribute
    {
        public SampleInfoAttribute()
        {
        }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? DisplayName { get; set; }
        public bool ApiKeyRequired { get; set; }
    }
    public class Sample
    {
        public Sample(Type page)
        {
            Page = page;
            var attr = page.GetTypeInfo().GetCustomAttribute(typeof(SampleInfoAttribute)) as SampleInfoAttribute;
            if (attr != null)
            {
                Name = attr.DisplayName;
                Category = attr.Category;
                Description = attr.Description;
                ApiKeyRequired = attr.ApiKeyRequired;
            }
            if (string.IsNullOrEmpty(Name))
            {
                //Deduce name from type name
                Name = Regex.Replace(Page.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0").Replace("Arc GIS", "ArcGIS").Replace("Geo View", "GeoView");
                if (Name.EndsWith("Sample"))
                    Name = Name.Substring(0, Name.Length - 6);
            }
            if (string.IsNullOrEmpty(Category))
            {
                if (Page.Namespace != null && !Page.Namespace.EndsWith(".Samples")) //use sub namespace instead
                {
                    Category = Page.Namespace.Substring(Page.Namespace.IndexOf(".Samples.") + 9);
                }
            }
        }

        public Type Page { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        public bool ApiKeyRequired { get; set; }
    }
}