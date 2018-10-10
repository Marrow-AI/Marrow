using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[ExecuteInEditMode]
public class HxDensityVolume : MonoBehaviour {

    public static HxOctree<HxDensityVolume> DensityOctree;
    public enum DensityShape { Square = 0, Sphere = 1, Cylinder = 2};
    public enum DensityBlendMode { Max = 0, Add = 1, Min = 2, Sub = 3 };
    HxOctreeNode<HxDensityVolume>.NodeObject octreeNode;

    public DensityShape Shape = DensityShape.Square;
    public DensityBlendMode BlendMode = DensityBlendMode.Add;
    [HideInInspector]
    public Vector3 minBounds;
    [HideInInspector]
    public Vector3 maxBounds;
    [HideInInspector]
    public Matrix4x4 ToLocalSpace;
    public float Density = 0.1f;

 
    // Use this for initialization
    void OnEnable()
    {
        CalculateBounds();
        if (DensityOctree == null) { DensityOctree = new HxOctree<HxDensityVolume>(); }
        HxVolumetricCamera.AllDensityVolumes.Add(this);
        octreeNode = DensityOctree.Add(this, minBounds, maxBounds);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "AreaLight Gizmo", true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    static Color gizmoColor = new Color(0.992f,0.749f,0.592f);

    void OnDisable()
    {

          HxVolumetricCamera.AllDensityVolumes.Remove(this);
        if (DensityOctree != null)
        {
            DensityOctree.Remove(this);
            DensityOctree = null;
        }
  
    }

    void OnDestroy()
    {

            HxVolumetricCamera.AllDensityVolumes.Remove(this);
        if (DensityOctree != null)
        {
            DensityOctree.Remove(this);
            DensityOctree = null;
        }

    }

    public void UpdateVolume()
    {
        if (transform.hasChanged)
        {
            CalculateBounds();
            DensityOctree.Move(octreeNode, minBounds, maxBounds);
            transform.hasChanged = false;
        }
    }
    
    static Vector3 c1 = new Vector3(0.5f, 0.5f, 0.5f);
    static Vector3 c2 = new Vector3(-0.5f, 0.5f, 0.5f);
    static Vector3 c3 = new Vector3(0.5f, 0.5f, -0.5f);
    static Vector3 c4 = new Vector3(-0.5f, 0.5f, -0.5f);
    static Vector3 c5 = new Vector3(0.5f, -0.5f, 0.5f);
    static Vector3 c6 = new Vector3(-0.5f, -0.5f, 0.5f);
    static Vector3 c7 = new Vector3(0.5f, -0.5f, -0.5f);
    static Vector3 c8 = new Vector3(-0.5f, -0.5f, -0.5f);


    void CalculateBounds()
    {


        Vector3 p1 = transform.TransformPoint(c1);
        Vector3 p2 = transform.TransformPoint(c2);
        Vector3 p3 = transform.TransformPoint(c3);
        Vector3 p4 = transform.TransformPoint(c4);
        Vector3 p5 = transform.TransformPoint(c5);
        Vector3 p6 = transform.TransformPoint(c6);
        Vector3 p7 = transform.TransformPoint(c7);
        Vector3 p8 = transform.TransformPoint(c8);

        minBounds = Vector3.Min(p1, Vector3.Min(p2, Vector3.Min(p3, Vector3.Min(p4, Vector3.Min(p5, Vector3.Min(p6, Vector3.Min(p7, p8)))))));
        maxBounds = Vector3.Max(p1, Vector3.Max(p2, Vector3.Max(p3, Vector3.Max(p4, Vector3.Max(p5, Vector3.Max(p6, Vector3.Max(p7, p8)))))));

        ToLocalSpace = transform.worldToLocalMatrix;
 
    }                                       

}
