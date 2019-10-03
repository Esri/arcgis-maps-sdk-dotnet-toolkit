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

namespace ARToolkit.SampleApp
{
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