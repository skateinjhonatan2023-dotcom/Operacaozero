using UnityEngine;

public sealed class OZMapChart : MonoBehaviour
{
    public Texture2D image;
    public Vector2 origin, size;
    public Vector3[] supplies;

    public Vector2 Coordinates(Vector3 world)
    {
        return new Vector2((world.x-origin.x)/size.x,(world.z-origin.y)/size.y);
    }
}
