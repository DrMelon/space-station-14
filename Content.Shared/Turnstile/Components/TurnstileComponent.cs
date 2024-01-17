using System.Numerics;
using Content.Shared.Turnstile.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;

namespace Content.Shared.Turnstile.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(TurnstileSystem))]
public sealed partial class TurnstileComponent : Component
{
    #region Spinner Physics

    /// <summary>
    /// The physics-driven spinner entity of the turnstile. It is managed by the TurnstileSystem.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public EntityUid SpinnerUid;

    /// <summary>
    /// Turnstile Spinner prototype ID.
    /// </summary>
    [DataField]
    public string SpinnerPrototype = "TurnstileSpinner";

    /// <summary>
    /// Turnstile Spinner relative anchor point.
    /// </summary>
    [DataField]
    public Vector2 SpinnerAnchorPoint = new Vector2(0.5f, 0.5f);

    #endregion

    #region Sounds
    /// <summary>
    /// Sound to play when the turnstile admits a mob through.
    /// </summary>
    [DataField]
    public SoundSpecifier? TurnSound;

    /// <summary>
    /// Sound to play when the turnstile is bumped from the wrong side
    /// </summary>
    [DataField]
    public SoundSpecifier? BumpSound;

    #endregion

}

[Serializable, NetSerializable]
public enum TurnstileVisuals : byte
{
    State,
    BaseRSI
}
