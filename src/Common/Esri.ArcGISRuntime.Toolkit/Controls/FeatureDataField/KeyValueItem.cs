using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    internal class KeyValueItem : INotifyPropertyChanged
    {
        private object _key;
        private string _value;

        public object Key
        {
            get { return _key; }
            set
            {
                if (value == _key) return;
                _key = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}