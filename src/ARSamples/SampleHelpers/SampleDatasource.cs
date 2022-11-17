using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp
{
    public class SampleDatasource
    {
        private static Type GetDefaultSampleType()
        {
#if MAUI
            return typeof(Page);
#else
#if NETFX_CORE
            return typeof(Windows.UI.Xaml.Controls.Page);
#elif __IOS__
            return typeof(UIKit.UIViewController);
#elif __ANDROID__
            return typeof(global::Android.App.Activity);
#endif
#endif
        }

        public SampleDatasource() : this(GetDefaultSampleType())
        {
        }

        public SampleDatasource(Type subclass, string nameFilter = ".Samples.")
        {
            var pages = from t in typeof(SampleDatasource).Assembly.ExportedTypes
                        where !t.IsAbstract && t.GetTypeInfo().IsSubclassOf(subclass) && t.FullName.Replace("Samples.Maui", "").Contains(nameFilter)
                        select t;


            Samples = (from p in pages select new Sample() { Type = p }).ToArray();
            foreach (var sample in Samples)
            {
                var attr = sample.Type.GetTypeInfo().GetCustomAttribute(typeof(SampleInfoAttribute)) as SampleInfoAttribute;
                if (attr != null)
                {
                    sample.Name = attr.DisplayName;
                    sample.Category = attr.Category;
                    sample.Description = attr.Description;
                }
                else if (!sample.Type.Namespace.EndsWith(".Samples")) //use sub namespace instead
                {
                    sample.Category = sample.Type.Namespace.Substring(sample.Type.Namespace.IndexOf(".Samples.") + 9);
                }
                if (string.IsNullOrEmpty(sample?.Name))
                {
#if __ANDROID__ && !MAUI
                    var actAttr = sample.Type.GetTypeInfo().GetCustomAttribute(typeof(global::Android.App.ActivityAttribute)) as global::Android.App.ActivityAttribute;
                    if (!string.IsNullOrEmpty(actAttr?.Label))
                        sample.Name = actAttr.Label;
                    else
#endif
                    {
                        //Deduce name from type name
                        sample.Name = Regex.Replace(sample.Type.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
#if __ANDROID__ && !MAUI
                        if (sample.Name.EndsWith("Activity"))
                            sample.Name = sample.Name.Substring(0, sample.Name.Length - 8);
#endif
                        if (sample.Name.EndsWith("Sample"))
                            sample.Name = sample.Name.Substring(0, sample.Name.Length - 6);
                        if (sample.Name.Contains("Arc GIS"))
                            sample.Name = sample.Name.Replace("Arc GIS", "ArcGIS");
                    }
                }
            }
        }

        public IList<Sample> Samples { get; private set; }


        public IEnumerable<SampleGroup> SamplesByCategory
        {
            get
            {
                var groups = (from item in Samples
                              orderby item.Category
                              group item by item.Category into g
                              select new SampleGroup(g) { Key = g.Key ?? "Misc" });
                return groups;
            }
        }
    }

    public class SampleGroup
    {
        public SampleGroup(IEnumerable<Sample> samples)
        {
            Items = samples;
        }
        public string Key { get; set; }

        public IEnumerable<Sample> Items { get; private set; }
    }

    public class Sample
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Category { get; set; }
        public bool IsDeviceSupported => true;
        public bool HasSampleData
        {
            get
            {
                var attr = Type.GetTypeInfo().GetCustomAttributes(typeof(SampleDataAttribute));
                return attr.Any();
            }
        }

        public async Task GetDataAsync(Action<string> progress)
        {
            var appDataFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

            foreach (var attr in Type.GetTypeInfo().GetCustomAttributes(typeof(SampleDataAttribute)).OfType<SampleDataAttribute>())
            {
                await attr.GetDataAsync(System.IO.Path.Combine(appDataFolder, attr.Path), progress);
            }
        }
    }
}
