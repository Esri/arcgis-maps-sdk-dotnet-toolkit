---
name: create-ui-tests
description: 'Create or update tests using this repository''s Appium-based UITests framework. Use when adding UI-based test coverage for a toolkit control, scaffolding from the UITest `dotnet new` template, modifying existing UI test logic, or wiring mirrored test pages.'
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

## Preferred path: use the UITest `dotnet new` template

For a new page or control test, use the Quick Start flow in `src\Tests\UITests\README.md` instead of creating files by hand. Treat the README as the canonical source for the exact `dotnet new install`, help, and generation commands.

What the template creates:

- shared page code-behind in `Toolkit.UITests.TestPages.Shared\`
- mirrored XAML pages for WPF, WinUI, and MAUI
- optional shared test class in `Toolkit.UITests.Shared\Tests\<ControlName>\` when `-T` is provided

If you only need an additional page for an existing test class, use the template without `-T` and add the new assertions to the existing shared test file manually.

Only fall back to manual page creation when the template cannot express the change. In that case, follow the manual guidance in `src\Tests\UITests\README.md` and reuse `Toolkit.UITests.TestPages.Shared\TestPage.cs`.

## How to add a new control test

Use an iterative workflow. Do **not** try to finish every mirrored page up front.

1. **Look for prior implementation first.** Start with `Compass` and `ScaleLine` tests and pages:
   - `src\Tests\UITests\Toolkit.UITests.Shared\Tests\Compass\CompassTests.cs`
   - `src\Tests\UITests\Toolkit.UITests.Shared\Tests\ScaleLine\ScaleLineTests.cs`
   - `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\CompassMap.xaml.cs`
   - `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\ScaleLines.xaml.cs`
2. **Choose the first platform based on the host OS.**
   - On **Windows**, start with **WPF**.
   - On **macOS**, start with **MAUI iOS**.
   - Treat that first platform as the design and validation surface for the initial test.
3. **Follow the README Quick Start** to install the local template, inspect its options if needed, and generate the starting files.
4. **Omit `-T`** when you are adding a page to an existing shared test class instead of creating a new test file.
5. **Create or refine only the shared code-behind and the first-platform page at first.** Keep the logic shared through `TestPage`, but do not spend time filling in the remaining mirrored pages yet.
6. **Add stable automation IDs for every element the test touches.** This is the default rule in this repo.
7. **Add or extend the shared MSTest class** in `Toolkit.UITests.Shared\Tests\<Control>\`.
8. **Open the page through the shared harness** with `OpenSample("<PageClassName>")`.
9. **Use the Appium helpers in `AppiumTestBase`** such as `FindElement`, `FindElements`, `Click`, `SubmitText`, and `GetScreenshot`.
10. **Keep tests small.** Favor a few deterministic steps over long end-to-end flows.
11. **Prefer lightweight image analysis over direct screenshot comparison** when visual verification is required.
12. **Stop after the first-platform test is working well enough for review.** Have the user review the changes and run the focused test(s) on that platform until the behavior and test shape are satisfactory.
13. **Only after the first-platform test is accepted, fill in the mirrored pages for the remaining platforms.** Keep those pages aligned to the reviewed first-platform surface rather than inventing platform-specific variants early.

## Required iteration loop

When using this skill for a new control test, follow this loop explicitly:

1. Build the shared test logic and one host-native page first.
2. Ask the user to review and run the focused test(s) for that first platform.
3. Incorporate feedback until the user is satisfied with the initial test page and assertions.
4. Mirror that approved test surface into the remaining platform pages.
5. Reuse the same shared test class unless a real platform-specific difference requires a narrow exception.

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

Use them only when you intentionally want an accessibility label or hint for assistive technologies

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

1. The first host-native platform page was built and reviewed before the mirrored pages were filled in.
2. The test page now exists for WPF, WinUI, and MAUI where applicable to the scenario.
3. The shared code-behind is reused where possible.
4. Every interacted-with element has an explicit automation ID.
5. The shared test uses `AppiumTestBase` helpers instead of ad hoc selectors.
6. The runner for the target platform is used with a focused MSTest filter first.
7. Any platform-specific differences are kept minimal and documented in code only where necessary.

## File references

- `src\Tests\UITests\README.md`
- `src\Tests\UITests\extensions\.template.config\template.json`
- `src\Tests\UITests\Directory.Build.props`
- `src\Tests\UITests\Toolkit.UITests.Shared\Appium\AppiumTestBase.Utils.cs`
- `src\Tests\UITests\Toolkit.UITests.Shared\Tests\Compass\CompassTests.cs`
- `src\Tests\UITests\Toolkit.UITests.TestPages.Shared\TestPage.cs`
