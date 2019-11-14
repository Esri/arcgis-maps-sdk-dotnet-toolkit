using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public class BookmarkItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool EditingEnabled => false;

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
