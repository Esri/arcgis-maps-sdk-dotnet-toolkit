using Esri.ArcGISRuntime.Mapping;
using System.Collections.Generic;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public class BookmarkItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private IList<Bookmark> _bookmarks;

        public IList<Bookmark> Bookmarks
        {
            get => _bookmarks;
            set
            {
                if (value != _bookmarks)
                {
                    _bookmarks = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bookmarks)));
                }
            }
        }
    }
}
