using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Esri.ArcGISRuntime.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Jobs
{
    [ExcludeFromSamples]
    public partial class JobViewControl : UserControl
    {
        public JobViewControl()
        {
            InitializeComponent();
            this.Loaded += JobViewControl_Loaded;
            this.Unloaded += JobViewControl_Unloaded;
        }

        private void JobViewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Job is not null)
            {
                Job.ProgressChanged -= Job_ProgressChanged;
            }
        }

        private void JobViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Job is not null)
            {
                Job.ProgressChanged += Job_ProgressChanged;
                UpdateJob();
            }
        }

        public IJob Job
        {
            get { return (IJob)GetValue(JobProperty); }
            set { SetValue(JobProperty, value); }
        }

        public static readonly DependencyProperty JobProperty =
            DependencyProperty.Register("Job", typeof(IJob), typeof(JobViewControl), new PropertyMetadata(null, (s, e) => ((JobViewControl)s).OnJobPropertyChanged(e.OldValue as IJob, e.NewValue as IJob)));

        private void OnJobPropertyChanged(IJob oldJob, IJob newJob)
        {
            if (oldJob is not null)
                oldJob.ProgressChanged -= Job_ProgressChanged;
            if (newJob != null && this.IsLoaded)
            {
                newJob.ProgressChanged += Job_ProgressChanged;
            }
            if (newJob != null)
                newJob.StatusChanged += Job_StatusChanged;
            UpdateJob();
            UpdateButtons();
        }

        private void Job_StatusChanged(object? sender, JobStatus e)
        {
            SafeDispatch(() =>
            {
                Status.Text = Job?.Status.ToString();
                UpdateButtons();
            });            
        }

        private void Job_ProgressChanged(object? sender, EventArgs e) => SafeDispatch(UpdateJob);

        private void SafeDispatch(Action action)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                return;
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
        }

        private void UpdateButtons()
        {
            ResumeButton.Visibility = Job?.Status == JobStatus.Paused ? Visibility.Visible : Visibility.Collapsed;
            PauseButton.Visibility = Job?.Status == JobStatus.Started ? Visibility.Visible : Visibility.Collapsed;
            DeleteButton.Visibility = Job?.Status == JobStatus.Failed || Job?.Status == JobStatus.Succeeded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateJob()
        {
            Status.Text = Job?.Status.ToString();
            Message.Text = Job?.Messages?.LastOrDefault()?.Message;
            ProgressBar.Value = Job?.Progress ?? 0;
        }

        private void PauseJob_Click(object sender, RoutedEventArgs e) => Job?.Pause();

        private void ResumeJob_Click(object sender, RoutedEventArgs e) => Job?.Start();

        private void DeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (Job is not null)
                JobManager.Shared.Jobs.Remove(Job);
        }
    }
}
