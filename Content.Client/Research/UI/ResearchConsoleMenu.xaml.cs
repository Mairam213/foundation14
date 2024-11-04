using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Research.UI;

[GenerateTypedNameReferences]
public sealed partial class ResearchConsoleMenu : FancyWindow
{
    public Action<string>? OnTechnologyCardPressed;
    public Action? OnServerButtonPressed;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly ResearchSystem _research;
    private readonly SpriteSystem _sprite;
    private readonly AccessReaderSystem _accessReader;

    public EntityUid Entity;

    public ResearchConsoleMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _research = _entity.System<ResearchSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _accessReader = _entity.System<AccessReaderSystem>();

        ServerButton.OnPressed += _ => OnServerButtonPressed?.Invoke();
    }

    public void SetEntity(EntityUid entity)
    {
        Entity = entity;
    }

    public void UpdatePanels(ResearchConsoleBoundInterfaceState state)
    {
        TechnologyCardsContainer.Children.Clear();

        var availableTech = _research.GetAvailableTechnologies(Entity);
        SyncTechnologyList(AvailableCardsContainer, availableTech);

        if (!_entity.TryGetComponent(Entity, out TechnologyDatabaseComponent? database))
            return;

        // i can't figure out the spacing so here you go
        TechnologyCardsContainer.AddChild(new Control
        {
            MinHeight = 10
        });

        var hasAccess = _player.LocalEntity is not { } local ||
                        !_entity.TryGetComponent<AccessReaderComponent>(Entity, out var access) ||
                        _accessReader.IsAllowed(local, Entity, access);
        foreach (var techId in database.CurrentTechnologyCards)
        {
            var tech = _prototype.Index<TechnologyPrototype>(techId);

            var availablePoints = state.Points;

            var cardControl = new TechnologyCardControl(tech, _prototype, _sprite, _research.GetTechnologyDescription(tech, includeTier: false), availablePoints, hasAccess);
            cardControl.OnPressed += () => OnTechnologyCardPressed?.Invoke(techId);
            TechnologyCardsContainer.AddChild(cardControl);
        }

        var unlockedTech = database.UnlockedTechnologies.Select(x => _prototype.Index<TechnologyPrototype>(x));
        SyncTechnologyList(UnlockedCardsContainer, unlockedTech);
    }

    public void UpdateInformationPanel(ResearchConsoleBoundInterfaceState state)
    {
        var totalPoints = string.Empty;

        foreach (var (pointType, value) in state.Points)
        {
            var pointPrototype = _prototype.Index<ResearchPointPrototype>(pointType);
            totalPoints += $"{Loc.GetString(pointPrototype.Name)}: {value}  ";
        }

        var amountMsg = new FormattedMessage();

        amountMsg.AddMarkupOrThrow(Loc.GetString("research-console-menu-research-points-text",
            ("points", totalPoints)));
        ResearchAmountLabel.SetMessage(amountMsg);

        if (!_entity.TryGetComponent(Entity, out TechnologyDatabaseComponent? database))
            return;

        var disciplineText = Loc.GetString("research-discipline-none");
        var disciplineColor = Color.Gray;
        if (database.MainDiscipline != null)
        {
            var discipline = _prototype.Index<TechDisciplinePrototype>(database.MainDiscipline);
            disciplineText = Loc.GetString(discipline.Name);
            disciplineColor = discipline.Color;
        }

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("research-console-menu-main-discipline",
            ("name", disciplineText), ("color", disciplineColor)));
        MainDisciplineLabel.SetMessage(msg);

        TierDisplayContainer.Children.Clear();
        foreach (var disciplineId in database.SupportedDisciplines)
        {
            var discipline = _prototype.Index<TechDisciplinePrototype>(disciplineId);
            var tier = _research.GetHighestDisciplineTier(database, discipline);

            // don't show tiers with no available tech
            if (tier == 0)
                continue;

            // i'm building the small-ass control here to spare me some mild annoyance in making a new file
            var texture = new TextureRect
            {
                TextureScale = new Vector2( 2, 2 ),
                VerticalAlignment = VAlignment.Center
            };
            var label = new RichTextLabel();
            texture.Texture = _sprite.Frame0(discipline.Icon);
            label.SetMessage(Loc.GetString("research-console-tier-info-small", ("tier", tier)));

            var control = new BoxContainer
            {
                Children =
                {
                    texture,
                    label,
                    new Control
                    {
                        MinWidth = 10
                    }
                }
            };
            TierDisplayContainer.AddChild(control);
        }
    }

    /// <summary>
    ///     Synchronize a container for technology cards with a list of technologies,
    ///     creating or removing UI cards as appropriate.
    /// </summary>
    /// <param name="container">The container which contains the UI cards</param>
    /// <param name="technologies">The current set of technologies for which there should be cards</param>
    private void SyncTechnologyList(BoxContainer container, IEnumerable<TechnologyPrototype> technologies)
    {
        // For the cards which already exist, build a map from technology prototype to the UI card
        var currentTechControls = new Dictionary<TechnologyPrototype, Control>();
        foreach (var child in container.Children)
        {
            if (child is MiniTechnologyCardControl)
            {
                currentTechControls.Add((child as MiniTechnologyCardControl)!.Technology, child);
            }
        }

        foreach (var tech in technologies)
        {
            if (!currentTechControls.ContainsKey(tech))
            {
                // Create a card for any technology which doesn't already have one.
                var mini = new MiniTechnologyCardControl(tech, _prototype, _sprite, _research.GetTechnologyDescription(tech));
                container.AddChild(mini);
            }
            else
            {
                // The tech already exists in the UI; remove it from the set, so we won't revisit it below
                currentTechControls.Remove(tech);
            }
        }

        // Now, any items left in the dictionary are technologies which were previously
        // available, but now are not. Remove them.
        foreach (var (tech, techControl) in currentTechControls)
        {
            container.Children.Remove(techControl);
        }
    }
}

