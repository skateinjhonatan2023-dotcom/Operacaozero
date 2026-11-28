using UnityEngine;

public sealed class OZGroundMotion
{
    readonly RaycastHit[] contacts = new RaycastHit[8];
    float lastContact = -10, jumpExpires = -10;

    public static Vector2 Joystick(Vector2 displacement, float radius)
    {
        Vector2 raw = displacement / Mathf.Max(1, radius);
        float magnitude = Mathf.InverseLerp(.09f, 1, raw.magnitude);
        // A shallow response near center gives precise walking without sacrificing full stick travel.
        return raw.normalized * (magnitude * Mathf.Lerp(.70f, 1, magnitude));
    }

    public static Vector3 Accelerate(Vector3 current, Vector3 goal, bool grounded, float dt)
    {
        bool braking = goal.sqrMagnitude < current.sqrMagnitude || Vector3.Dot(current,goal) < 0;
        float acceleration = grounded ? (braking ? 22f : 12f) : 3.6f;
        return Vector3.MoveTowards(current,goal,acceleration*dt);
    }

    public bool Surface(CharacterController capsule, out RaycastHit surface)
    {
        var root = capsule.transform;
        float radius = capsule.radius * .78f;
        Vector3 origin = root.position + Vector3.up*(radius+.14f);
        int count = Physics.SphereCastNonAlloc(origin,radius,Vector3.down,contacts,.34f,~0,QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        float minY = Mathf.Cos(capsule.slopeLimit*Mathf.Deg2Rad);
        surface = default;
        for (int i=0;i<count;i++)
        {
            var hit=contacts[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(root) || hit.normal.y<minY || hit.distance>=best) continue;
            surface=hit;best=hit.distance;
        }
        return best<float.PositiveInfinity;
    }

    public float Vertical(float velocity, bool grounded, bool low, bool jump, float now, float dt)
    {
        if(grounded && velocity<=0) lastContact=now;
        if(jump) jumpExpires=now+.11f;
        if(!low && now<=jumpExpires && now-lastContact<=.09f)
        {
            Reset();
            return 4.95f;
        }
        return grounded && velocity<=0 ? -2.4f : Mathf.Max(-36,velocity-20.4f*dt);
    }
    public void Reset() {jumpExpires=lastContact=-10;}
}
