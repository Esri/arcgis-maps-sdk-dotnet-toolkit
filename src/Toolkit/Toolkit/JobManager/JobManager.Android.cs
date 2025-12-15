#if ANDROID
using Android.Content;

namespace  Esri.ArcGISRuntime.Toolkit
{
    public sealed partial class JobManager
    {
        private JobManager(string? id)
        {
            _id = id;
            Init();
        }

        private void SaveState(string json)
        {
            var context = Android.App.Application.Context;
            var prefs = context.GetSharedPreferences("ArcGIS.JobManager", FileCreationMode.Private);
            var editor = prefs.Edit();
            editor.PutString(DefaultsKey, json);
            editor.Apply();
        }

        private string? LoadStateInternal()
        {
            var context = Android.App.Application.Context;
            var prefs = context.GetSharedPreferences("ArcGIS.JobManager", FileCreationMode.Private);
            return prefs.GetString(DefaultsKey, null);
        }
    }
}
#endif