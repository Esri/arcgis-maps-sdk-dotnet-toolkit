namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class FeatureFormViewResources : ResourceDictionary
{
    public FeatureFormViewResources()
    {
        InitializeComponent();
    }

    private void FeatureCandidateSearch_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter &&
            sender is Microsoft.UI.Xaml.Controls.TextBox
            {
                DataContext: Primitives.UtilityAssociationFeatureCandidateSelection selection,
            } &&
            selection.SearchCommand.CanExecute(null))
        {
            selection.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}
