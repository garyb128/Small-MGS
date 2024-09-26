using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceProperties : MonoBehaviour
{
    public enum SurfaceType
    {
        Normal,
        Snow,
        Noisy,
        Fire
    }
    
    public SurfaceType surfaceType;
    public bool isWalkable;
}
