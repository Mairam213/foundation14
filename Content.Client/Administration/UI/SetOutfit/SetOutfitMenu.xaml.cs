using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.UI.SetOutfit
{
    [GenerateTypedNameReferences]
    public sealed partial class SetOutfitMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        public NetEntity? TargetEntityId { get; set; }
        private StartingGearPrototype? _selectedOutfit;

        public SetOutfitMenu()
        {
            MinSize = SetSize = new Vector2(250, 320);
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            Title = Loc.GetString("set-outfit-menu-title");

            ConfirmButton.Text = Loc.GetString("set-outfit-menu-confirm-button");
            ConfirmButton.OnPressed += ConfirmButtonOnOnPressed;
            SearchBar.OnTextChanged += SearchBarOnOnTextChanged;
            OutfitList.OnItemSelected += OutfitListOnOnItemSelected;
            OutfitList.OnItemDeselected += OutfitListOnOnItemDeselected;
            PopulateList();
        }

        private void ConfirmButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (TargetEntityId == null || _selectedOutfit == null)
                return;
            var command = $"setoutfit {TargetEntityId} {_selectedOutfit.ID}";
            _consoleHost.ExecuteCommand(command);
            Close();
        }

        private void OutfitListOnOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            _selectedOutfit = (StartingGearPrototype) obj.ItemList[obj.ItemIndex].Metadata!;
            ConfirmButton.Disabled = false;
        }

        private void OutfitListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
        {
            _selectedOutfit = null;
            ConfirmButton.Disabled = true;
        }


        private void SearchBarOnOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            PopulateByFilter(SearchBar.Text);
        }

        private IEnumerable<StartingGearPrototype> GetPrototypes()
        {
            // Filter out any StartingGearPrototypes that belong to loadouts
            var loadouts = _prototypeManager.EnumeratePrototypes<LoadoutPrototype>();
            var loadoutGears = loadouts.Select(l => l.StartingGear);
            return _prototypeManager.EnumeratePrototypes<StartingGearPrototype>()
                .Where(p => !loadoutGears.Contains(p.ID));
        }

        private void PopulateList()
        {
            foreach (var gear in GetPrototypes())
            {
                OutfitList.Add(GetItem(gear, OutfitList));
            }
        }

        private void PopulateByFilter(string filter)
        {
            OutfitList.Clear();
            foreach (var gear in GetPrototypes())
            {
                if (!string.IsNullOrEmpty(filter) &&
                    gear.ID.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant()))
                {
                    OutfitList.Add(GetItem(gear, OutfitList));
                }
            }
        }


        private static ItemList.Item GetItem(StartingGearPrototype gear, ItemList itemList)
        {
            return new(itemList)
            {
                Metadata = gear,
                Text = gear.ID
            };
        }
    }
}
