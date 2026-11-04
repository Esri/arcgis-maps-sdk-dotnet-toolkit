using Esri.ArcGISRuntime.UI;
using Windows.UI;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class SceneLightingPage : Component
{
    private static readonly Camera InitialCamera = new Camera(37.7152, -119.6776, 2400, 76, 90, 0);
    private static readonly TimeSpan YosemiteTimeOffset = TimeSpan.FromHours(-7);

    private readonly (string Name, DateTimeOffset Value)[] sunTimeOptions =
    [
        ("Sunrise", new DateTimeOffset(2026, 6, 2, 5, 15, 0, YosemiteTimeOffset)),
        ("Morning", new DateTimeOffset(2026, 6, 2, 8, 30, 0, YosemiteTimeOffset)),
        ("Noon", new DateTimeOffset(2026, 6, 2, 12, 0, 0, YosemiteTimeOffset)),
        ("Sunset", new DateTimeOffset(2026, 6, 2, 20, 30, 0, YosemiteTimeOffset)),
        ("Night", new DateTimeOffset(2026, 6, 2, 21, 30, 0, YosemiteTimeOffset)),
    ];

    private readonly (string Name, Color Value)[] ambientLightOptions =
    [
        ("White", Color.FromArgb(255, 255, 255, 255)),
        ("Warm", Color.FromArgb(255, 255, 244, 214)),
        ("Cool", Color.FromArgb(255, 214, 232, 255)),
        ("Twilight", Color.FromArgb(255, 156, 175, 255)),
        ("Night", Color.FromArgb(255, 96, 122, 178)),
    ];

    private readonly Scene scene = new Scene(BasemapStyle.ArcGISImageryStandard)
    {
        InitialViewpoint = new Viewpoint(InitialCamera.Location, InitialCamera)
    }.WorldElevation();

    public override Element Render()
    {
        var sunLightingOptions = Enum.GetValues<LightingMode>();
        var atmosphereOptions = Enum.GetValues<AtmosphereEffect>();
        var spaceOptions = Enum.GetValues<SpaceEffect>();

        var defaultSunLightingIndex = Array.IndexOf(sunLightingOptions, LightingMode.LightAndShadows);
        var defaultAtmosphereEffectIndex = Array.IndexOf(atmosphereOptions, AtmosphereEffect.Realistic);

        var (selectedSunLightingIndex, setSelectedSunLightingIndex) = UseState(defaultSunLightingIndex >= 0 ? defaultSunLightingIndex : 0);
        var (selectedSunTimeIndex, setSelectedSunTimeIndex) = UseState(0);
        var (selectedAtmosphereEffectIndex, setSelectedAtmosphereEffectIndex) = UseState(defaultAtmosphereEffectIndex >= 0 ? defaultAtmosphereEffectIndex : 0);
        var (selectedSpaceEffectIndex, setSelectedSpaceEffectIndex) = UseState(0);
        var (selectedAmbientLightIndex, setSelectedAmbientLightIndex) = UseState(0);

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            SceneView(scene) with
            {
                SunLighting = sunLightingOptions[selectedSunLightingIndex],
                SunTime = sunTimeOptions[selectedSunTimeIndex].Value,
                AtmosphereEffect = atmosphereOptions[selectedAtmosphereEffectIndex],
                SpaceEffect = spaceOptions[selectedSpaceEffectIndex],
                AmbientLightColor = ambientLightOptions[selectedAmbientLightIndex].Value,
            },
            GalleryControls.ControlPanel(
                VStack(12,
                    ComboBox(
                            items: sunLightingOptions.Select(mode => mode.ToString()).ToArray(),
                            selectedIndex: selectedSunLightingIndex,
                            onSelectedIndexChanged: index => setSelectedSunLightingIndex(index))
                        .Header("Sun lighting")
                        .Width(220),
                    ComboBox(
                            items: sunTimeOptions.Select(option => option.Name).ToArray(),
                            selectedIndex: selectedSunTimeIndex,
                            onSelectedIndexChanged: index => setSelectedSunTimeIndex(index))
                        .Header("Sun time")
                        .Width(220),
                    ComboBox(
                            items: atmosphereOptions.Select(effect => effect.ToString()).ToArray(),
                            selectedIndex: selectedAtmosphereEffectIndex,
                            onSelectedIndexChanged: index => setSelectedAtmosphereEffectIndex(index))
                        .Header("Atmosphere effect")
                        .Width(220),
                    ComboBox(
                            items: spaceOptions.Select(effect => effect.ToString()).ToArray(),
                            selectedIndex: selectedSpaceEffectIndex,
                            onSelectedIndexChanged: index => setSelectedSpaceEffectIndex(index))
                        .Header("Space effect")
                        .Width(220),
                    ComboBox(
                            items: ambientLightOptions.Select(option => option.Name).ToArray(),
                            selectedIndex: selectedAmbientLightIndex,
                            onSelectedIndexChanged: index => setSelectedAmbientLightIndex(index))
                        .Header("Ambient light")
                        .Width(220))));
    }
}
