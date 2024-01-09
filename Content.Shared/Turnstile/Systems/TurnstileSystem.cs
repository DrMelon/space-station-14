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
        revoluteJoint.LocalAnchorB = new Vector2(0.0f, 0.0f);
        revoluteJoint.CollideConnected = false;

        // We need it to only spin in one direction, like a ratchet; Box2D convention is that you simply increase the
        // minimum and maximum angle limit as the joint rotates in the desired direction.
        revoluteJoint.EnableLimit = true;
        revoluteJoint.ReferenceAngle = 0f;
        revoluteJoint.LowerAngle = 0f;
        revoluteJoint.UpperAngle = Single.DegreesToRadians(359.0f);

        // Additionally, setting a motor with speed 0 and a low torque allows the joint to slow to a halt when not being
        // actively turned. We'll want this too, so that turnstiles don't just freewheel forever.
        revoluteJoint.EnableMotor = true;
        revoluteJoint.MaxMotorTorque = 0.001f;
        revoluteJoint.MotorSpeed = 0f;

        Dirty(ent, jointComp);
        Dirty(ent.Comp1.SpinnerUid, spinnerJointComp);
    }

    /// <summary>
    ///     Iterate over turnstiles and update the ratcheting angle.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TurnstileComponent, JointComponent>();
        while (query.MoveNext(out var uid, out var turnstile, out var jointComp))
        {
            if (jointComp.GetJoints.TryGetValue(TurnstileJointId, out var joint))
            {
                if (joint is RevoluteJoint revoluteJoint)
                {
                    revoluteJoint.LowerAngle = revoluteJoint.GetCurrentAngle();
                    revoluteJoint.UpperAngle = revoluteJoint.GetCurrentAngle() + float.DegreesToRadians(1080.0f);
                }
            }
        }
    }
}
