using UnityEngine;

// The controller owns the root. Only the visual rig and the grip follow the evaluated pose.
public sealed class OZFieldRig : MonoBehaviour
{
    public Transform body,spine,leftToe,rightToe,rightHand,leftHand,rightKnuckle,leftKnuckle,grip;
    public AvatarMask upperMask;
    public AnimationClip jog,aimWalk,back,left,right,takeoff,land;
    public RuntimeAnimatorController groundedController;
    // Sole points calibrated against the boot mesh in the bind pose (toe-local coordinates).
    public Vector3 leftSole,rightSole;
    public bool airborne,reloading;
    Vector3 rest;
    Quaternion wristOffset;
    bool hasGrip;
    public bool adjustFootContacts;
    readonly RaycastHit[] contacts = new RaycastHit[8];
    sealed class Leg
    {
        public Transform hip,knee,ankle,toe;
        public Transform support;
        public Vector3 anchor,soleOffset;
        public float weight;
    }
    Leg leftLeg,rightLeg;
    bool bound;
    void Awake() => BindPose();
    public void BindPose()
    {
        if(bound)return;
        bound=true;
        rest=body.localPosition;
        var animator=body.GetComponentInChildren<Animator>();
        if(!animator || !animator.isHuman)return;
        Leg Bind(HumanBodyBones thigh,HumanBodyBones shin,HumanBodyBones foot,Transform toe)
            => new Leg {hip=animator.GetBoneTransform(thigh),knee=animator.GetBoneTransform(shin),
                ankle=animator.GetBoneTransform(foot),toe=toe};
        leftLeg=Bind(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,leftToe);
        rightLeg=Bind(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,rightToe);
        leftLeg.soleOffset=leftSole;rightLeg.soleOffset=rightSole;
    }

    void PlaceFoot(Leg leg,float lowest,float dt)
    {
        if(leg==null)return;
        if(airborne){leg.support=null;leg.weight=0;return;}
        Vector3 sole=leg.toe.TransformPoint(leg.soleOffset);
        int count=Physics.RaycastNonAlloc(sole+Vector3.up*.27f,Vector3.down,contacts,.55f,~0,QueryTriggerInteraction.Ignore);
        int support=-1;
        for(int i=0;i<count;i++)
        {
            if(contacts[i].normal.y<.7f || contacts[i].transform.IsChildOf(transform))continue;
            if(support<0 || contacts[i].distance<contacts[support].distance)support=i;
        }
        if(support<0){leg.support=null;leg.weight=0;return;}
        var hit=contacts[support];
        float phase=1-Mathf.InverseLerp(.025f,.12f,sole.y-lowest);
        leg.weight=Mathf.MoveTowards(leg.weight,phase,dt*12);
        Vector3 targetSole=hit.point+.004f*Vector3.up;
        if(leg.support!=hit.transform || phase<.55f ||
            Vector3.ProjectOnPlane(leg.support.TransformPoint(leg.anchor)-targetSole,Vector3.up).sqrMagnitude>.0144f)
        {
            leg.support=hit.transform;
            leg.anchor=leg.support.InverseTransformPoint(targetSole);
        }
        Vector3 planted=leg.support.TransformPoint(leg.anchor);
        Vector3 goal=leg.ankle.position+Vector3.ClampMagnitude(planted-sole,.16f)*leg.weight;
        // Rotate the existing segments about their joints, retaining the animated knee bend plane.
        Vector3 hip=leg.hip.position,knee=leg.knee.position,ankle=leg.ankle.position;
        float a=(knee-hip).magnitude,b=(ankle-knee).magnitude;
        Vector3 axis=(goal-hip).normalized;
        Vector3 bend=Vector3.ProjectOnPlane(knee-hip,axis);
        if(bend.sqrMagnitude<.00001f || a<.02f || b<.02f)return;
        float reach=Mathf.Clamp((goal-hip).magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
        float along=(a*a-b*b+reach*reach)/(2*reach);
        Vector3 newKnee=hip+axis*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Quaternion footRotation=leg.ankle.rotation;
        leg.hip.rotation=Quaternion.FromToRotation(knee-hip,newKnee-hip)*leg.hip.rotation;
        leg.knee.rotation=Quaternion.FromToRotation(leg.ankle.position-leg.knee.position,hip+axis*reach-leg.knee.position)*leg.knee.rotation;
        leg.ankle.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,hit.normal),leg.weight*.65f)*footRotation;
    }

    void LateUpdate() => EvaluateContacts(Time.deltaTime);
    public void EvaluateContacts(float dt)
    {
        BindPose();
        float sole=Mathf.Min(leftToe.TransformPoint(leftSole).y,rightToe.TransformPoint(rightSole).y);
        float lift=airborne ? 0 : Mathf.Clamp(body.localPosition.y-rest.y+transform.position.y+.004f-sole,-.26f,.28f);
        body.localPosition=rest+Vector3.up*Mathf.Lerp(body.localPosition.y-rest.y,lift,1-Mathf.Exp(-25*dt));
        if(adjustFootContacts)
        {
            float lowest=Mathf.Min(leftToe.TransformPoint(leftSole).y,rightToe.TransformPoint(rightSole).y);
            PlaceFoot(leftLeg,lowest,dt); PlaceFoot(rightLeg,lowest,dt);
        }
        Vector3 palm=Vector3.Lerp(rightHand.position,rightKnuckle.position,.5f);
        Vector3 direction=Vector3.Lerp(leftHand.position,leftKnuckle.position,.43f)-palm;
        Quaternion rotation=hasGrip ? rightHand.rotation*wristOffset : transform.rotation;
        if ((!reloading || !hasGrip) && direction.sqrMagnitude>.012f)
        {
            rotation=Quaternion.LookRotation(direction,transform.up);
            wristOffset=Quaternion.Inverse(rightHand.rotation)*rotation;
            hasGrip=true;
        }
        grip.SetPositionAndRotation(palm,rotation);
    }
}
