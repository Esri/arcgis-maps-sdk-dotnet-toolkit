using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public class SampleInfoAttributeAttribute : Attribute
    {
        public SampleInfoAttributeAttribute()
        {
        }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
    }
    public class SampleDatasource
    {
        private SampleDatasource()
        {


            var pages = from t in typeof(SampleDatasource).Assembly.ExportedTypes
                        where t.GetTypeInfo().IsSubclassOf(typeof(Android.App.Activity)) && t.FullName.Contains(".Samples.")
                        select t;

            Samples = (from p in pages select new Sample() { Activity = p }).ToArray();
            foreach (var sample in Samples)
            {
                var attr = sample.Activity.GetTypeInfo().GetCustomAttribute(typeof(SampleInfoAttributeAttribute)) as SampleInfoAttributeAttribute;
                if (attr != null)
                {
                    sample.Name = attr.DisplayName;
                    sample.Category = attr.Category;
                    sample.Description = attr.Description;
                }
                else if (!sample.Activity.Namespace.EndsWith(".Samples")) //use sub namespace instead
                {
                    sample.Category = sample.Activity.Namespace.Substring(sample.Activity.Namespace.IndexOf(".Samples.") + 9);
                }
                if (string.IsNullOrEmpty(sample?.Name))
                {
                    var actAttr = sample.Activity.GetTypeInfo().GetCustomAttribute(typeof(ActivityAttribute)) as ActivityAttribute;
                    if (!string.IsNullOrEmpty(actAttr?.Label))
                        sample.Name = actAttr.Label;
                    else
                    {
                        //Deduce name from type name
                        sample.Name = Regex.Replace(sample.Activity.Name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
                        if (sample.Name.EndsWith("Activity"))
                            sample.Name = sample.Name.Substring(0, sample.Name.Length - 8);
                        if (sample.Name.EndsWith("Sample"))
                            sample.Name = sample.Name.Substring(0, sample.Name.Length - 6);
                        if (sample.Name.Contains("Arc GIS"))
                            sample.Name = sample.Name.Replace("Arc GIS", "ArcGIS");
                    }
                }
            }
        }

        public IList<Sample> Samples { get; private set; }

        private static SampleDatasource m_Current;
        public static SampleDatasource Current
        {
            get
            {
                if (m_Current == null)
                    m_Current = new SampleDatasource();
                return m_Current;
            }
        }

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
        public Type Activity { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Category { get; set; }
    }

    internal class SampleScreenAdapter : BaseAdapter<Sample>, ISectionIndexer
    {
        private SampleDatasource items;
        private List<Sample> sampleItems;
        private Activity context;

        public SampleScreenAdapter(Activity context, SampleDatasource items) : base()
        {
            this.context = context;
            sampleItems = new List<Sample>();
            foreach (var item in items.SamplesByCategory)
            {
                sampleItems.AddRange(item.Items);
            }
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override Sample this[int position]
        {
            get
            {
                return sampleItems[position];
            }
        }
        public override int Count
        {
            get { return sampleItems.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);

            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = sampleItems[position].Name;
            var subHeader = sampleItems[position].Description ?? "";
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = subHeader;
            return view;
        }

        public int GetPositionForSection(int sectionIndex)
        {
            int i = 0;
            int count = 0;
            foreach (var item in items.SamplesByCategory)
            {
                if (i >= sectionIndex)
                    break;
                count += item.Items.Count();
                i++;
            }
            return count;
        }

        public int GetSectionForPosition(int position)
        {
            int i = 0;
            int count = 0;
            foreach (var item in items.SamplesByCategory)
            {
                if (i > position)
                    return count;
                foreach (var ite2m in item.Items)
                {
                    i++;
                }
                count++;
            }
            return count;
        }

        public Java.Lang.Object[] GetSections()
        {
            return (items.SamplesByCategory.Select(t => new Java.Lang.String(t.Key)).ToArray());
        }
    }
}