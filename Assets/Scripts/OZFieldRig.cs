using UnityEngine;

// The controller owns the root. Only the visual rig and the grip follow the evaluated pose.
public sealed class OZFieldRig : MonoBehaviour
{
    public Transform body,spine,leftToe,rightToe,rightHand,leftHand,rightKnuckle,leftKnuckle,grip;
    public AvatarMask upperMask;
    public AnimationClip jog,aimWalk,back,left,right;
    public bool airborne,reloading;
    Vector3 rest;
    Quaternion wristOffset;
    bool hasGrip;
    void Awake() => rest=body.localPosition;

    void LateUpdate()
    {
        float sole=Mathf.Min(leftToe.position.y,rightToe.position.y);
        float lift=airborne ? 0 : Mathf.Clamp(body.localPosition.y-rest.y+transform.position.y+.025f-sole,-.26f,.28f);
        body.localPosition=rest+Vector3.up*Mathf.Lerp(body.localPosition.y-rest.y,lift,1-Mathf.Exp(-25*Time.deltaTime));
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
