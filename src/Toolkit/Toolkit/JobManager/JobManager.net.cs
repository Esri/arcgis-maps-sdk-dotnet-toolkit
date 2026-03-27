#if !ANDROID && !__IOS__ && !WINDOWS
#if MAUI

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
            Preferences.Default.Set(DefaultsKey, json);
        }

        private string? LoadStateInternal()
        {
            return Preferences.Default.Get(DefaultsKey, (string?)null);
        }
    }
}
#endif
#endif