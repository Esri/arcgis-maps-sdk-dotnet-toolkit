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
                UpdateJob(Job);
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
            UpdateJob(newJob);
        }
        private static void Job_StatusChanged(object? sender, JobStatus e)
        {
            if (e == JobStatus.Succeeded || e == JobStatus.Failed)
            {

            }
        }
        private void Job_ProgressChanged(object? sender, EventArgs e)
        {
            UpdateJob(sender as IJob);
        }

        private void UpdateJob(IJob? job)
        {
            Dispatcher.Invoke(() =>
            {
                Status.Text = job?.Status.ToString();
                Message.Text = job?.Messages?.LastOrDefault()?.Message;
                ProgressBar.Value = job?.Progress ?? 0;
            });
        }
    }
}
