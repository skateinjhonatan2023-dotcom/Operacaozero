using UnityEngine;
public sealed class OZDoor : MonoBehaviour
{
    bool open;
    Quaternion closed;
    void Awake() => closed=transform.localRotation;
    public void Toggle() => open=!open;
    void Update()
    {
        var target=closed*Quaternion.Euler(0,open?95:0,0);
        transform.localRotation=Quaternion.RotateTowards(transform.localRotation,target,180f*Time.deltaTime);
    }
}