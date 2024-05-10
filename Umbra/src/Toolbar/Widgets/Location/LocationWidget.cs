﻿/* Umbra | (c) 2024 by Una              ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra is free software: you can redistribute  \/     \/             \/
 *     it and/or modify it under the terms of the GNU Affero General Public
 *     License as published by the Free Software Foundation, either version 3
 *     of the License, or (at your option) any later version.
 *
 *     Umbra UI is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using Umbra.Common;
using Umbra.Game;
using Umbra.Interface;

namespace Umbra.Toolbar.Widgets.Location;

[Service]
internal partial class LocationWidget : IToolbarWidget
{
    [ConfigVariable("Toolbar.Widget.Location.Enabled", "EnabledWidgets")]
    private static bool Enabled { get; set; } = true;

    [ConfigVariable("Toolbar.Widget.Location.ItemSpacing", "ToolbarSettings", "ToolbarCustomization", min: 0, max: 600)]
    private static int ItemSpacing { get; set; } = 0;

    private readonly IPlayer      _player;
    private readonly IZoneManager _zoneManager;

    private uint _lastWeatherIconId = 0;

    public LocationWidget(IPlayer player, IZoneManager zoneManager, ToolbarPopupContext ctx)
    {
        _player      = player;
        _zoneManager = zoneManager;

        ctx.RegisterDropdownActivator(Element.Get("Icon"), _dropdownElement);
    }

    public void OnDraw()
    {
        if (!Enabled) {
            Element.IsVisible = false;
            return;
        }

        Element.IsVisible = true;
        UpdateToolbarWidget();
        UpdateDropdownWidget();
    }

    private void UpdateToolbarWidget()
    {
        IZone            zone    = _zoneManager.CurrentZone;
        WeatherForecast? weather = zone.CurrentWeather;

        (string zoneName, string distName) = GetLocationNames();

        Element.Get("Location").Margin                       = new(right: ItemSpacing);
        Element.Get("Location.Name.Label").Text              = zoneName;
        Element.Get("Location.Info").Text                    = distName;
        Element.Get("Location.Name.SanctuaryIcon").IsVisible = zone.IsSanctuary;

        if (weather == null) {
            // This should theoretically never happen, but just in case.
            return;
        }

        string weatherName = weather.Name;

        if (_player.HomeWorldName != _player.CurrentWorldName) {
            weatherName = $"{_player.CurrentWorldName} - {weatherName}";
        }

        Element.Get("Weather.Name").Text = weatherName;
        Element.Get("Weather.Info").Text = weather.TimeString[..1].ToUpper() + weather.TimeString[1..];
        Element.Get("Icon").Style.Image  = weather.IconId;

        if (_lastWeatherIconId != weather.IconId) {
            _lastWeatherIconId                = weather.IconId;
            Element.Get("Icon").Padding       = new(-16); // Enlarges the icon without causing a reflow.
            Element.Get("Icon").Style.Opacity = 0;
            Element.Get("Icon").Animate(new Animation<InOutCirc>(300) { Padding = new(0), Opacity = 1 });
        }
    }

    public void OnUpdate() { }

    private void UpdateDropdownWidget()
    {
        if (!_dropdownElement.IsVisible) return;

        IZone            zone    = _zoneManager.CurrentZone;
        WeatherForecast? weather = zone.CurrentWeather;

        if (null == weather) return;

        (string zoneName, string distName)                      = GetLocationNames();
        _dropdownElement.Get("Header.Content.Icon").Style.Image = weather.IconId;
        _dropdownElement.Get("Header.Content.Text.Name").Text   = zoneName;
        _dropdownElement.Get("Header.Content.Text.Info").Text   = $"{distName}, {weather.Name}";

        var gradTop = _dropdownElement.Get<GradientElement>("Header.BG");
        var gradBot = _dropdownElement.Get<GradientElement>("BG");

        var color    = ImageRepository.GetIconFile(weather.IconId).GetDominantColor();
        var topColor = color.ToUint().ApplyAlphaComponent(0.35f);
        var botColor = color.ToUint().ApplyAlphaComponent(0.45f);

        gradTop.Gradient = Gradient.Vertical(0,        topColor);
        gradBot.Gradient = Gradient.Vertical(botColor, 0);

        for (int i = 0; i < 6; i++) {
            var el = _dropdownElement.Get("ForecastList.Forecast" + (i));

            if (i + 1 < zone.WeatherForecast.Count) {
                WeatherForecast forecast = zone.WeatherForecast[i + 1];
                el.IsVisible               = true;
                el.Get("Icon").Style.Image = forecast.IconId;
                el.Get("Text.Name").Text   = forecast.Name;
                el.Get("Text.Info").Text   = $"{I18N.Translate("WeatherForecast.In")} {forecast.TimeString}.";
            } else {
                el.IsVisible = false;
            }
        }
    }

    private (string, string) GetLocationNames()
    {
        var zone = _zoneManager.CurrentZone;

        string zoneName = zone.Name;
        string distName = zone.CurrentDistrictName;

        if (distName == "" && zoneName.Contains(" - ")) {
            string[] split = zoneName.Split(" - ");
            zoneName = split[1];
            distName = split[0];
        }

        // Fallback to region name if zone name is empty. This may occur in an Inn room.
        if (zoneName == "") zoneName = zone.RegionName;
        if (distName == "") distName = zone.RegionName;

        return (zoneName, distName);
    }
}
