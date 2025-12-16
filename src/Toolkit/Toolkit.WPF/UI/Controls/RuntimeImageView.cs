using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public sealed class RuntimeImageView : Image
    {
        public RuntimeImage? RuntimeImage
        {
            get { return GetValue(RuntimeImageProperty) as RuntimeImage; }
            set { SetValue(RuntimeImageProperty, value); }
        }

        public static readonly DependencyProperty RuntimeImageProperty =
            DependencyProperty.Register(nameof(RuntimeImage), typeof(RuntimeImage), typeof(RuntimeImageView), new PropertyMetadata(null, ((s,e)=> ((RuntimeImageView)s).OnRuntimeImagePropertyChanged())));

        private async void OnRuntimeImagePropertyChanged()
        {
            this.Source = null;
            try
            {
                this.Source = await RuntimeImageExtensions.ToImageSourceAsync(RuntimeImage);
            }
            catch { }
        }
    }
}
