using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;


namespace ARToolkit.SampleApp
{
    internal class Utilities
    {

        public static async Task<string> UnpackAssetData(Context context, string filename)
        {
            var folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SampleData");
            var path = System.IO.Path.Combine(folder, filename);
            //if (!System.IO.File.Exists(path))
            {
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);
                using (var file = System.IO.File.Create(path))
                {
                    using (var sr = Application.Context.Assets.Open(filename))
                    {
                        await sr.CopyToAsync(file);
                        await file.FlushAsync();
                    }
                }
            }
            return path;
        }
    }

}