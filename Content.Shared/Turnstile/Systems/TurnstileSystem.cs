using System.Numerics;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Turnstile.Systems;

public sealed class TurnstileSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly INetManager _net = default!;

    private const string TurnstileJointId = "turnstile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<TurnstileComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.SpinnerUid = EntityUid.Invalid;
        if (_net.IsServer)
            CreateSpinner(ent);
    }


    private void CreateSpinner(Entity<TurnstileComponent, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        if (ent.Comp1.SpinnerUid != null && ent.Comp1.SpinnerUid != EntityUid.Invalid)
            return;

        // Create Spinner entity, which this turnstile will use.
        var spinnerId = SpawnAtPosition(ent.Comp1.SpinnerPrototype.Id, ent.Comp2.Coordinates);
        ent.Comp1.SpinnerUid = spinnerId;

        // Attach the Spinner using a revolute joint.
        var jointComp = EnsureComp<JointComponent>(ent);
        var spinnerJointComp = EnsureComp<JointComponent>(spinnerId);
        var revoluteJoint = _joints.CreateRevoluteJoint(ent, spinnerId, TurnstileJointId);

        // Set up revolute joint settings.
        revoluteJoint.LocalAnchorA = ent.Comp1.SpinnerAnchorPoint;
        revoluteJoint.LocalAnchorB = new Vector2(0.5f, 0.0f);
        revoluteJoint.CollideConnected = false;

        // We need it to open to a maximum of 90 degrees; we don't want it opening any further. We also don't want it to
        // rotate into the negative, so we give it a minimum angle of 0 degrees.
        revoluteJoint.EnableLimit = true;
        revoluteJoint.ReferenceAngle = 0f;
        revoluteJoint.LowerAngle = 0f;
        revoluteJoint.UpperAngle = Single.DegreesToRadians(90.0f);

        Dirty(ent, jointComp);
        Dirty(spinnerId, spinnerJointComp);
    }
}
