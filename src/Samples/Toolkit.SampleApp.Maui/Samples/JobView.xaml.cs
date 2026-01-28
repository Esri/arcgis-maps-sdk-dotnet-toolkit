using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Toolkit;

namespace Toolkit.SampleApp.Maui;

public partial class JobView : ContentView
{
	public JobView()
	{
		InitializeComponent();
        this.Loaded += JobView_Loaded;
        this.Unloaded += JobView_Unloaded;
	}

    private void JobView_Loaded(object? sender, EventArgs e)
    {
        if (Job is not null)
        {
            Job.ProgressChanged += Job_ProgressChanged;
            UpdateJob();
        }
    }

    private void JobView_Unloaded(object? sender, EventArgs e)
    {
        if (Job is not null)
        {
            Job.ProgressChanged -= Job_ProgressChanged;
        }
    }
    public IJob Job
    {
        get { return (IJob)GetValue(JobProperty); }
        set { SetValue(JobProperty, value); }
    }

    public static readonly BindableProperty JobProperty =
        BindableProperty.Create("Job", typeof(IJob), typeof(JobView), null, propertyChanged: (s, oldJob, newJob) => ((JobView)s).OnJobPropertyChanged(oldJob as IJob, newJob as IJob));

    private void OnJobPropertyChanged(IJob? oldJob, IJob? newJob)
    {
        if (oldJob is not null)
        {
            oldJob.StatusChanged -= Job_StatusChanged;
            oldJob.ProgressChanged -= Job_ProgressChanged;
        }
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
        Dispatcher.Dispatch(() =>
        {
            Status.Text = Job?.Status.ToString();
            UpdateButtons();
        });
    }

    private void Job_ProgressChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(UpdateJob);

    private void UpdateButtons()
    {
        ResumeButton.IsVisible = Job?.Status == JobStatus.Paused;
        PauseButton.IsVisible = Job?.Status == JobStatus.Started;
        DeleteButton.IsVisible = Job?.Status == JobStatus.Failed || Job?.Status == JobStatus.Succeeded;
    }

    private void UpdateJob()
    {
        Status.Text = Job?.Status.ToString();
        Message.Text = Job?.Messages?.LastOrDefault()?.Message;
        ProgressBar.Progress = (Job?.Progress ?? 0) * 0.01;
    }

    private void PauseJob_Click(object sender, EventArgs e) => Job?.Pause();

    private void ResumeJob_Click(object sender, EventArgs e) => Job?.Start();

    private void DeleteJob_Click(object sender, EventArgs e)
    {
        if (Job is not null)
            JobManager.Shared.Jobs.Remove(Job);
    }
}