#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Globalization;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// TimePicker used by <see cref="DateTimePickerFormInput"/>.
    /// </summary>
    public class TimePicker : Control
    {
        private TextBox? _timeText;

        /// <summary>
        /// Initializes an instance of the <see cref="TimePicker"/> class.
        /// </summary>
        public TimePicker()
        {
            DefaultStyleKey = typeof(TimePicker);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _timeText = GetTemplateChild("TimeText") as TextBox;
            if(_timeText != null)
            {
                _timeText.LostFocus += TimeText_LostFocus;
                _timeText.KeyDown += TimeText_KeyDown;
            }
            UpdateText();
            base.OnApplyTemplate();
        }

        private void TimeText_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                TryParseText();
            }
        }

        private void TimeText_LostFocus(object sender, RoutedEventArgs e) => TryParseText();

        private void TryParseText()
        {
            if (_timeText is null)
                return;
            if(DateTime.TryParse(_timeText.Text, null, DateTimeStyles.NoCurrentDateDefault, out DateTime result))
            {
                Time = result.TimeOfDay;
            }
            else
            {
                UpdateText(); //Reset text
            }
        }

        private void UpdateText()
        {
            if (_timeText != null)
            {
                if (Time.HasValue)
                {
                    _timeText.Text = new DateTime(1, 1, 1).Add(Time.Value).ToString("T");
                }
                else
                    _timeText.Text = null;
            }
        }

        /// <summary>
        /// Gets or sets the displayed time.
        /// </summary>
        public TimeSpan? Time
        {
            get { return (TimeSpan?)GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Time"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(TimeSpan?), typeof(TimePicker), new PropertyMetadata(null,((s,e) => ((TimePicker)s).OnTimePropertyChanged())));

        private void OnTimePropertyChanged()
        {
            UpdateText();
            TimeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raised when the <see cref="Time"/> property changes
        /// </summary>
        public event EventHandler? TimeChanged;
    }
}
#endif