// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    internal class KeyValuePair<TKey,TValue> : INotifyPropertyChanged
    {
        #region Private Members

        private TKey _key;
        private TValue _value;

        #endregion Private Members

        #region Constructors

        public KeyValuePair(){ }
        public KeyValuePair(TKey key, TValue value) { _key = key; _value = value; }

        #endregion Constructors

        #region Public Properties

        #region Key

        public TKey Key
        {
            get { return _key; }
            set
            {
                if ((value == null && _key == null) || _key.Equals(value)) return;                
                _key = value;
                OnPropertyChanged();
            }
        }

        #endregion Key

        #region Value

        public TValue Value
        {
            get { return _value; }
            set
            {
                if ((value == null && _value == null) || _value.Equals(value)) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        #endregion Value

        #endregion Public Properteis

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}