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
        if(_net.IsServer)
            CreateSpinner(ent);
    }


    private void CreateSpinner(Entity<TurnstileComponent, TransformComponent?> ent)
    {
        if (!Resolve<TransformComponent>(ent, ref ent.Comp2))
            return;

        if (ent.Comp1.SpinnerUid != EntityUid.Invalid)
            return;

        // Create Spinner entity, which this turnstile will use.
        ent.Comp1.SpinnerUid = EntityManager.SpawnEntity(ent.Comp1.SpinnerPrototype, ent.Comp2.Coordinates);

        // Attach the Spinner using a revolute joint.
        var jointComp = EnsureComp<JointComponent>(ent);
        var spinnerJointComp = EnsureComp<JointComponent>(ent.Comp1.SpinnerUid);
        var revoluteJoint = _joints.CreateRevoluteJoint(ent, ent.Comp1.SpinnerUid, TurnstileJointId);

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
        Dirty(ent.Comp1.SpinnerUid, spinnerJointComp);
    }
}
