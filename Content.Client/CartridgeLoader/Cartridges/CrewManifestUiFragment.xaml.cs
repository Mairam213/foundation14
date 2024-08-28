﻿using Content.Client.CrewManifest.UI;
using Content.Shared.CrewManifest;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class CrewManifestUiFragment : BoxContainer
{
    public CrewManifestUiFragment()
    {
        RobustXamlLoader.Load(this);

        StationName.AddStyleClass("LabelBig");
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    public void UpdateState(string stationName, CrewManifestEntries? entries)
    {
        CrewManifestListing.DisposeAllChildren();
        CrewManifestListing.RemoveAllChildren();

        StationNameContainer.Visible = entries != null;
        StationName.Text = stationName;

        if (entries == null)
            return;

        CrewManifestListing.AddCrewManifestEntries(entries);
    }
}
