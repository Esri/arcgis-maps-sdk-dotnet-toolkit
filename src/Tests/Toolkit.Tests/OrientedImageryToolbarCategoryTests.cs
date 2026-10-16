using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Toolkit.Tests;

[TestClass]
public sealed class OrientedImageryToolbarCategoryTests
{
    [STATestMethod]
    public void AddedToolbarItemsConnectToTheCategoryViewModel()
    {
        var model = new OrientedImageryViewModel();
        var category = model.ToolbarCategories[0];
        var item = new AllowAddingMarkersVM();

        category.ToolbarItems.Add(item);

        Assert.AreSame(model, item.ViewModel);
        item.IsAddingMarkersEnabled = true;
        Assert.AreEqual(OrientedImageryViewInteractionState.AddMarkers, model.InteractionState);

        category.ToolbarItems.Remove(item);

        Assert.IsNull(item.ViewModel);
    }

    [STATestMethod]
    public void ReplacingAndClearingToolbarItemsDisconnectsRemovedItems()
    {
        var model = new OrientedImageryViewModel();
        var category = model.ToolbarCategories[0];
        var original = category.ToolbarItems[0];
        var replacement = new ImageSearchModeVM();

        category.ToolbarItems[0] = replacement;

        Assert.IsNull(original.ViewModel);
        Assert.AreSame(model, replacement.ViewModel);
        replacement.SelectedInteractionState = OrientedImageryViewInteractionState.ExamineImage;
        Assert.AreEqual(OrientedImageryViewInteractionState.ExamineImage, model.InteractionState);

        category.ToolbarItems.Clear();

        Assert.IsNull(replacement.ViewModel);
    }
}
