using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Sunrise.ServersHub;

[GenerateTypedNameReferences]
public sealed partial class ServersHubUi : DefaultWindow
{
    [Dependency] private readonly ServersHubManager _serversHubManager = default!;

    public ServersHubUi()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void RefreshHeader()
    {
        var totalPlayers = _serversHubManager.ServersDataList.Sum(server => server.CurrentPlayers);
        var maxPlayers = _serversHubManager.ServersDataList.Sum(server => server.MaxPlayers);
        ServersHubHeaderLabel.Text = Loc.GetString("serverhub-playingnow", ("total", totalPlayers), ("max", maxPlayers));
    }
}
