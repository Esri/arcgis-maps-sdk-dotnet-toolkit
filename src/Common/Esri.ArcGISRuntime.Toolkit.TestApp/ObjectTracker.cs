using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Code coming from Callipso WinStore toolkit: https://github.com/timheuer/callisto

namespace Esri.ArcGISRuntime.Toolkit.TestApp
{
    public static class ObjectTracker
    {
        private static readonly object _Monitor = new object();
        private static readonly List<WeakReference> _Objects = new List<WeakReference>();
        private static bool? _ShouldTrack;

        public static void Track(object objectToTrack)
        {
            if (ShouldTrack())
            {
                lock (_Monitor)
                {
                    _Objects.Add(new WeakReference(objectToTrack));
                }
            }
        }

        private static bool ShouldTrack()
        {
            if (_ShouldTrack == null)
            {
                _ShouldTrack = Debugger.IsAttached;
            }

            //return _ShouldTrack.Value;
	        return true; // todo
        }

        public static IEnumerable<object> GetAllLiveTrackedObjects()
        {
            lock (_Monitor)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                return _Objects.Where(o => o.IsAlive).Select(o => o.Target);
            }
        }

		public static string GarbageCollect()
        {
            // Garbage Collect
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var liveObjects = ObjectTracker.GetAllLiveTrackedObjects();

            StringBuilder sbStatus = new StringBuilder();

            Debug.WriteLine("---------------------------------------------------------------------");
            if (liveObjects.Count() == 0)
            {
                sbStatus.AppendLine("No Memory Leaks.");
            }
            else
            {
                sbStatus.AppendLine("***    Possible memory leaks in the objects below or their children.   ***");
                sbStatus.AppendLine("*** Clear memory again and see if any of the objects free from memory. ***");
            }
            foreach (object obj in liveObjects)
            {
                string strAliveObj = obj.GetType().ToString();
                sbStatus.AppendLine(strAliveObj);
            }
            sbStatus.AppendLine("----");
            //long lBytes = GC.GetTotalMemory(true);
            //sbStatus.AppendLine(string.Format("GC.GetTotalMemory(true): {0} Bytes, {1} MB", lBytes.ToString(), (lBytes / 1024 / 1024).ToString()));
            Debug.WriteLine(sbStatus.ToString());
            Debug.WriteLine("---------------------------------------------------------------------");
			return sbStatus.ToString();
        }
    }
}
