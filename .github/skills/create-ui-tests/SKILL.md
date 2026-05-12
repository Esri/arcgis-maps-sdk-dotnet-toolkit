---
name: create-ui-tests
description: 'Create or update Appium-based UI tests for this repository''s UITests projects. Use when scaffolding a new UITest with the `dotnet new arcgis-toolkit-uitest` template, adding coverage for a toolkit control, wiring mirrored test pages, checking Appium dependencies, or deciding whether AutomationId, AutomationProperties, or SemanticProperties are needed.'
---

# Create UI Tests

Use this skill when working in `src\Tests\UITests\` to add or update Appium UI tests for toolkit controls.

## What this repo expects

- UI tests are Appium-driven MSTest projects.
- Shared test logic lives in `src\Tests\UITests\Toolkit.UITests.Shared\`.
- Shared test page code-behind lives in `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\`.
- Mirrored XAML pages live in:
  - `src\Tests\UITests\Toolkit.UITests.Wpf.App\TestPages\`
  - `src\Tests\UITests\Toolkit.UITests.WinUI.App\TestPages\`
  - `src\Tests\UITests\Toolkit.UITests.Maui.App\TestPages\`
- Test pages should derive from `Toolkit.UITests.App.TestPages.TestPage`.
- New pages are discovered automatically by reflection in each test app. There is no manual registration list. Keep the page in the `.TestPages.` namespace and make it derive from the correct platform base (`UserControl` on WPF/WinUI via `TestPage`, `ContentView` on MAUI via `TestPage`).

## Preferred path: scaffold from the UITest template

For a new page or control test, start with the repo's `dotnet new` template instead of creating the files by hand.

Install the local template from the repository root:

```powershell
dotnet new install .\src\Tests\UITests\extensions\
```

Inspect the available arguments:

```powershell
dotnet new arcgis-toolkit-uitest -h
```

Generate the new UITest files into `src\Tests\UITests\`:

```powershell
dotnet new arcgis-toolkit-uitest --output .\src\Tests\UITests\ -C <ControlName> -P <PageName> -T <TestsName>
```

Parameter meanings:

- `-C` / `ControlName`: folder and control name, such as `Compass` or `FeatureFormView`
- `-P` / `PageName`: test page name, such as `CompassMap` or `FeatureFormViewForms`
- `-T` / `TestsName`: optional shared MSTest file name, such as `CompassTests` or `FeatureFormViewTests_Accessibility`

What the template creates:

- shared page code-behind in `Toolkit.UITests.TestPages.Shared\`
- mirrored XAML pages for WPF, WinUI, and MAUI
- optional shared test class in `Toolkit.UITests.Shared\Tests\<ControlName>\` when `-T` is provided

If you only need an additional page for an existing test class, omit `-T` and add the new assertions to the existing shared test file manually.

## How to add a new control test

1. **Look for prior art first.** Start with `Compass` and `ScaleLine` tests and pages:
   - `src\Tests\UITests\Toolkit.UITests.Shared\Tests\Compass\CompassTests.cs`
   - `src\Tests\UITests\Toolkit.UITests.Shared\Tests\ScaleLine\ScaleLineTests.cs`
   - `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\CompassMap.xaml.cs`
   - `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\ScaleLines.xaml.cs`
2. **Install the local UITest template**:
   - `dotnet new install --force .\src\Tests\UITests\extensions\`
3. **Generate the starting files with the template**:
   - `dotnet new arcgis-toolkit-uitest --output .\src\Tests\UITests\ -C <ControlName> -P <PageName> -T <TestsName>`
   - Omit `-T` when you are adding a page to an existing shared test class instead of creating a new test file.
4. **Refine the generated shared test page code-behind** in `Toolkit.UITests.TestPages.Shared\`. Keep the logic shared through `TestPage`.
5. **Refine the generated mirrored XAML pages** in the WPF, WinUI, and MAUI app projects so they expose the same test surface.
6. **Add stable automation IDs for every element the test touches.** This is the default rule in this repo.
7. **Add or extend the shared MSTest class** in `Toolkit.UITests.Shared\Tests\<Control>\`.
8. **Open the page through the shared harness** with `OpenSample("<PageClassName>")`.
9. **Use the Appium helpers in `AppiumTestBase`** such as `FindElement`, `FindElements`, `Click`, `SubmitText`, and `GetScreenshot`.
10. **Keep tests small.** Favor a few deterministic steps over long end-to-end flows.
11. **Prefer lightweight image analysis over direct screenshot comparison** when visual verification is required.

## Accessibility and element identity rules

### Use explicit IDs by default

If a test will interact with or inspect a control, define an explicit automation identifier on that control.

- **WPF / WinUI:** use `AutomationProperties.AutomationId="..."`.
- **MAUI:** use `AutomationId="..."`.

Examples already in the repo:

- WPF / WinUI `ScaleLines.xaml` sets `AutomationProperties.AutomationId` on `MapView` and `ScaleLine`.
- MAUI `CompassMap.xaml` and `ScaleLines.xaml` set `AutomationId` on the controls the tests query.

### Do not rely on `x:Name` alone for cross-platform tests

- Windows runners use `MobileBy.AccessibilityId(...)`.
- Non-Windows runners use `MobileBy.Id(...)`.
- Some controls may be findable from `x:Name` on Windows, but that does not make the ID stable across MAUI platforms.

For cross-platform coverage, set the automation ID explicitly instead of assuming the framework will expose `x:Name` the same way everywhere.

### When to use `SemanticProperties`

`SemanticProperties` are **not** the primary mechanism used by the current UITest suite.

Use them only when:

- you intentionally want an accessibility label or hint for assistive technologies

Do **not** use `SemanticProperties` as a substitute for `AutomationId` when the test can identify the control by ID. In this repo, ID-based lookup is the preferred and more stable approach.

## Writing the shared test

Follow the patterns in `AppiumTestBase`:

- `OpenSample("PageName")` loads the page by class name.
- `FindElement("Id")` is the normal lookup path.
- `FindElement(parent, "ChildId")` is the normal path for child elements.
- `FindElementByName("Text")` is a fallback, not the first choice.

Prefer assertions based on:

- explicit UI state exposed through IDs,
- control text retrieved through the helper methods, or
- targeted image analysis when rendering itself is the feature under test.

## Implementation checklist

Before considering the work complete, make sure all of these are true:

1. The test page exists for WPF, WinUI, and MAUI.
2. The shared code-behind is reused where possible.
3. Every interacted-with element has an explicit automation ID.
4. The shared test uses `AppiumTestBase` helpers instead of ad hoc selectors.
5. The runner for the target platform is used with a focused MSTest filter first.
6. Any platform-specific differences are kept minimal and documented in code only where necessary.

## File references

- `src\Tests\UITests\README.md`
- `src\Tests\UITests\extensions\.template.config\template.json`
- `src\Tests\UITests\Directory.Build.props`
- `src\Tests\UITests\Toolkit.UITests.Shared\Appium\AppiumTestBase.Utils.cs`
- `src\Tests\UITests\Toolkit.UITests.Shared\Tests\Compass\CompassTests.cs`
- `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\TestPage.cs`
