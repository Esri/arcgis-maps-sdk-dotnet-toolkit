using System.Windows.Automation.Peers;
using System.Windows.Controls;
using HarmonyLib;

namespace Toolkit.UITests.Wpf.Puppet;

/// <summary>
/// This class uses the Harmony library to patch WPF controls for improved testability.
/// </summary>
internal class ControlPatcher
{
    public static void ApplyPatches()
    {
        var harmony = new Harmony("com.toolkit.tests.wpf.controlpatcher");

        // Patch for TextBlockAutomationPeer.IsContentElementCore()
        var original = typeof(TextBlockAutomationPeer).GetMethod("IsControlElementCore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var postfix = typeof(ControlPatcher).GetMethod(nameof(TextBlockAutomationPeer_IsContentElementCore_Postfix));
        harmony.Patch(original, postfix: new HarmonyMethod(postfix));
    }

    /// <summary>
    /// Ensures TextBlocks are all considered content elements by the WAD. By default they are hidden when inside a ContentPresenter (see https://github.com/dotnet/wpf/blob/8b3fd67094fb4d7b50aa0848497deaa1de3f1981/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/TextBlockAutomationPeer.cs#L64).
    /// More info: https://github.com/microsoft/WinAppDriver/issues/1249
    /// </summary>
    public static void TextBlockAutomationPeer_IsContentElementCore_Postfix(UIElementAutomationPeer __instance, ref bool __result)
    {
        if (__instance?.Owner is TextBlock)
        {
            __result = true;
        }
    }
}