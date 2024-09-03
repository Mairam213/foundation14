﻿using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp939;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp939VisibilityComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan LastTimeDoSomething { get; set; }

    public float HideTime { get; set; } = 2f;
}
