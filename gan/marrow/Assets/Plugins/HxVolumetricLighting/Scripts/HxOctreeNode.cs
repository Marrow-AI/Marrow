using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class HxOctreeNode<T>
{
    public Vector3 Origin { get; private set; }
    public float Size { get; private set; }
    public HxOctreeNode<T> Parent = null;

    float MinSize;
    float Overlap; // Percentage overlap between nodes (0-1)
    float SizeWithOverlap;
    public Vector3 BoundsMin;
    public Vector3 BoundsMax;

    [System.Serializable]
    public class NodeObject
    {
        public HxOctreeNode<T> Node;
        public T Value;
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
        public Vector3 Center;

        public NodeObject(T value, Vector3 boundsMin, Vector3 boundsMax)
        {
            Value = value;
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
            Center = (BoundsMax + BoundsMin) / 2f;
        }
    }

    readonly List<NodeObject> Objects = new List<NodeObject>();
    const int MaxObjectCount = 8;

    public HxOctreeNode<T>[] Children = null;
    Vector3[] ChildrenBoundsMin;
    Vector3[] ChildrenBoundsMax;

    public int ID;
    static int _idCtr = 0;
    public HxOctreeNode(float size, float overlap, float minSize, Vector3 origin, HxOctreeNode<T> parent)
    {
        ID = _idCtr++;
        Init(size, overlap, minSize, origin, parent);
    }

    void Init(float size, float overlap, float minSize, Vector3 origin, HxOctreeNode<T> parent)
    {
        Parent = parent;
        Size = size;
        MinSize = minSize;
        Overlap = overlap;
        Origin = origin;
        SizeWithOverlap = (1f + Overlap) * Size;
        Vector3 extents = new Vector3(SizeWithOverlap, SizeWithOverlap, SizeWithOverlap) / 2f;
        BoundsMin = Origin - extents;
        BoundsMax = Origin + extents;

        Vector3 childExtents = (Vector3.one * (Size / 2f) * (1f + Overlap)) / 2f;
        float offset = Size / 4f;

        ChildrenBoundsMin = new Vector3[8];
        ChildrenBoundsMax = new Vector3[8];
        Vector3 o;
        o = Origin + new Vector3(-1, 1, -1) * offset;
        ChildrenBoundsMin[0] = o - childExtents;
        ChildrenBoundsMax[0] = o + childExtents;
        o = Origin + new Vector3(1, 1, -1) * offset;
        ChildrenBoundsMin[1] = o - childExtents;
        ChildrenBoundsMax[1] = o + childExtents;
        o = Origin + new Vector3(-1, 1, 1) * offset;
        ChildrenBoundsMin[2] = o - childExtents;
        ChildrenBoundsMax[2] = o + childExtents;
        o = Origin + new Vector3(1, 1, 1) * offset;
        ChildrenBoundsMin[3] = o - childExtents;
        ChildrenBoundsMax[3] = o + childExtents;
        o = Origin + new Vector3(-1, -1, -1) * offset;
        ChildrenBoundsMin[4] = o - childExtents;
        ChildrenBoundsMax[4] = o + childExtents;
        o = Origin + new Vector3(1, -1, -1) * offset;
        ChildrenBoundsMin[5] = o - childExtents;
        ChildrenBoundsMax[5] = o + childExtents;
        o = Origin + new Vector3(-1, -1, 1) * offset;
        ChildrenBoundsMin[6] = o - childExtents;
        ChildrenBoundsMax[6] = o + childExtents;
        o = Origin + new Vector3(1, -1, 1) * offset;
        ChildrenBoundsMin[7] = o - childExtents;
        ChildrenBoundsMax[7] = o + childExtents;
    }

    public void Add(NodeObject node)
    {
        if (Objects.Count < MaxObjectCount || Size < MinSize * 2f)
        {
            node.Node = this;
            Objects.Add(node);
        }
        else
        {
            int index;
            if (Children == null)
            {
                float childSize = Size / 2f;
                float offset = Size / 4f;
                Children = new HxOctreeNode<T>[8];
                Children[0] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(-1, 1, -1) * offset, this);
                Children[1] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(1, 1, -1) * offset, this);
                Children[2] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(-1, 1, 1) * offset, this);
                Children[3] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(1, 1, 1) * offset, this);
                Children[4] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(-1, -1, -1) * offset, this);
                Children[5] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(1, -1, -1) * offset, this);
                Children[6] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(-1, -1, 1) * offset, this);
                Children[7] = new HxOctreeNode<T>(childSize, Overlap, MinSize, Origin + new Vector3(1, -1, 1) * offset, this);

                // Reconfigure the objects in this node to the appropriate child nodes
                for (int i = Objects.Count - 1; i >= 0; i--)
                {
                    NodeObject obj = Objects[i];
                    index = OctantIndex(obj.Center);
                    if (BoundsContains(Children[index].BoundsMin, Children[index].BoundsMax, obj.BoundsMin, obj.BoundsMax))
                    {
                        Children[index].Add(obj);
                        Objects.Remove(obj);
                    }
                }
            }

            // add the new object
            index = OctantIndex(node.Center);
            if (BoundsContains(Children[index].BoundsMin, Children[index].BoundsMax, node.BoundsMin, node.BoundsMax))
            {
                Children[index].Add(node);
            }
            else
            {
                node.Node = this;
                Objects.Add(node);
            }
        }
    }

    /// <summary>
    /// Returns true if the object was removed
    /// </summary>
    public bool Remove(T value)
    {
        bool removed = false;

        for (int i = 0; i < Objects.Count; i++)
        {
            if (Objects[i].Value.Equals(value))
            {
                removed = Objects.Remove(Objects[i]);
                break;
            }
        }

        if (!removed && Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Children[i].Remove(value))
                {
                    removed = true;
                    break;
                }
            }
        }

        if (removed && Children != null)
        {
            // try to merge the children into the current node
            int count = Objects.Count;
            if (Children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    var child = Children[i];
                    if (child.Children != null) // too many objects
                    {
                        return removed;
                    }
                    count += child.Objects.Count;
                }
            }
            if (count <= MaxObjectCount)
            {
                for (int i = 0; i < 8; i++)
                {
                    var objects = Children[i].Objects;
                    for (int j = 0; j < objects.Count; j++)
                    {
                        objects[j].Node = this;
                        Objects.Add(objects[j]);
                    }
                }
                Children = null;
            }
        }
        return removed;
    }

    /// <summary>
    /// Get all objects within the bounds
    /// </summary>
    public void GetObjects(Vector3 boundsMin, Vector3 boundsMax, List<T> items)
    {
        if (!BoundsIntersects(boundsMin, boundsMax, this.BoundsMin, this.BoundsMax))
        {
            return;
        }

        for (int i = 0; i < Objects.Count; i++)
        {
            if (BoundsIntersects(boundsMin, boundsMax, Objects[i].BoundsMin, Objects[i].BoundsMax))
            {
                items.Add(Objects[i].Value);
            }
        }

        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].GetObjects(boundsMin, boundsMax, items);
            }
        }
    }

    /// <summary>
    /// Get all objects within the bounds
    /// </summary>
    public void GetObjects2(Vector3 boundsMin, Vector3 boundsMax, List<T> items)
    {
        if (!BoundsIntersects(boundsMin, boundsMax, this.BoundsMin, this.BoundsMax))
        {
            return;
        }
        // If the bounds contains the entire node, just add everything
        if (BoundsContains(boundsMin, boundsMax, this.BoundsMin, this.BoundsMax))
        {
            addAllObjectsToList(items);
            return;
        }

        for (int i = 0; i < Objects.Count; i++)
        {
            if (BoundsIntersects(Objects[i].BoundsMin, Objects[i].BoundsMax, boundsMin, boundsMax))
            {
                items.Add(Objects[i].Value);
            }
        }

        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].GetObjects2(boundsMin, boundsMax, items);
            }
        }
    }

    public void GetObjects2BoundsPlane(ref Plane[] planes, Vector3 boundsMin, Vector3 boundsMax, List<T> items)
    {
        if (!BoundsIntersects(boundsMin, boundsMax, this.BoundsMin, this.BoundsMax))
        {
            return;
        }
        // If the bounds contains the entire node, just add everything
        if (BoundsContains(boundsMin, boundsMax, this.BoundsMin, this.BoundsMax))
        {
            if (BoundsInPlanes(boundsMin, boundsMax, ref planes) == 2) 
            {
                addAllObjectsToList(items);
                return;
            }     
        }

        for (int i = 0; i < Objects.Count; i++)
        {
            if (BoundsIntersects(Objects[i].BoundsMin, Objects[i].BoundsMax, boundsMin, boundsMax))
            {
                if(BoundsInPlanes(Objects[i].BoundsMin, Objects[i].BoundsMax, ref planes) >= 1)
                {
                    items.Add(Objects[i].Value);
                }
            }
        }

        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].GetObjects2BoundsPlane(ref planes,boundsMin, boundsMax, items);
            }
        }
    }

   void DrawBounds(Vector3 min, Vector3 max)
    {
        Debug.DrawLine(min, new Vector3(min.x, min.y, max.z),Color.red);
        Debug.DrawLine(min, new Vector3(min.x, max.y, min.z), Color.red);
        Debug.DrawLine(min, new Vector3(max.x, min.y, min.z), Color.red);
    }

    int BoundsInPlanes(Vector3 min, Vector3 max,ref Plane[] planes)
    {

        int result = 2;
        //for each plane do ...
        for (int i = 0; i < planes.Length; i++)
        {
            // is the positive vertex outside?
            if (planes[i].GetDistanceToPoint(GetVertexP(min, max, planes[i].normal)) < 0)
            {
                
                return 0;
            }
            // is the negative vertex outside?
            else if (planes[i].GetDistanceToPoint(GetVertexN(min, max, planes[i].normal)) < 0)
                result = 1;
        }
     
        return (result);
    }

    bool ObjectInPlanes(Vector3 min, Vector3 max,ref Plane[] planes)
    {
        //for each plane do ...
        for (int i = 0; i < planes.Length; i++)
        {
            // is the positive vertex outside?
            if (!planes[i].GetSide(GetVertexP(min, max, planes[i].normal)))
                return false;
        }
        return true;
    }

    Vector3 GetVertexP(Vector3 min,Vector3 max, Vector3 normal)
    {
        if (normal.x > 0)
		    min.x = max.x;
        if (normal.y > 0)
		    min.y = max.y;
	    if (normal.z > 0)
		    min.z = max.z;
            return min;
    }

    Vector3 GetVertexN(Vector3 min, Vector3 max, Vector3 normal)
    {
        if (normal.x > 0)
            max.x = min.x;
        if (normal.y > 0)
            max.y = min.y;
        if (normal.z > 0)
            max.z = min.z;
        return max;
    }

    void addAllObjectsToList(List<T> items)
    {
        for (int i = 0; i < Objects.Count; i++)
        {
            items.Add(Objects[i].Value);
        }
        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].addAllObjectsToList(items);
            }
        }
    }

    void addAllObjectsToList(List<T> items,ref Vector3 min, ref Vector3 max)
    {
        for (int i = 0; i < Objects.Count; i++)
        {
            items.Add(Objects[i].Value);
            min = new Vector3(Mathf.Min(min.x, Objects[i].BoundsMin.x), Mathf.Min(min.y, Objects[i].BoundsMin.y), Mathf.Min(min.z, Objects[i].BoundsMin.z));
            max = new Vector3(Mathf.Max(max.x, Objects[i].BoundsMax.x), Mathf.Max(max.y, Objects[i].BoundsMax.y), Mathf.Max(max.z, Objects[i].BoundsMax.z));
        }
        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].addAllObjectsToList(items,ref min, ref max);
            }
        }
    }

      
    /// <summary>
    /// Make this node smaller if possible.  Returns the new root, if applicable
    /// </summary>
    public HxOctreeNode<T> TryShrink(float minSize)
    {
        if (Size < (2 * minSize)) // already small enough
        {
            return this;
        }
        if (Objects.Count == 0 && (Children == null || Children.Length == 0))
        {
            // has no objects
            return this;
        }



        int index = -1;
        for (int i = 0; i < Objects.Count; i++)
        {
            var obj = Objects[i];
            int newIndex = OctantIndex(obj.Center);
            if (i == 0 || newIndex == index) // the children must be all in the same octant for shrinking to occur
            {
                if (BoundsContains(ChildrenBoundsMin[newIndex], ChildrenBoundsMax[newIndex], obj.BoundsMin, obj.BoundsMax))
                {
                    if (index < 0)
                    {
                        index = newIndex;
                    }
                }
                else
                {
                    // object doesnt fit in octant
                    return this;
                }
            }
            else
            {
                return this; // objects in different octants
            }
        }

        if (Children != null)
        {
            bool hasObjects = false;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i].HasObjects())
                {
                    if (hasObjects)
                    {
                        return this; // another child already has objects 
                    }
                    if (index >= 0 && index != i)
                    {
                        return this; // objects are in different octants
                    }
                    hasObjects = true;
                    index = i;
                }
            }
        }

        if (Children == null) // no children, so just shrink to a child-size octant
        {
            //shrink node
            Init(Size / 2f, Overlap, MinSize, (ChildrenBoundsMin[index] + ChildrenBoundsMax[index]) / 2f, Parent);
            return this;
        }
        // pick the child with objects in it.  add the current obj to that one.
        for (int i = 0; i < Objects.Count; i++)
        {
            var obj = Objects[i];
            Children[index].Add(obj);
        }
        // no objects found
        if (index < 0)
        {
            return this;
        }

        // promote the child node
        Children[index].Parent = this.Parent;
        return Children[index];
    }

    Vector3 GetVertexP(Vector3 normal)
    {
        return Vector3.zero;
    }

    /// <summary>
    /// If objects are in this node or in child nodes.
    /// </summary>
    /// 
    bool HasObjects()
    {
        if (Objects.Count > 0) return true;

        if (Children != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Children[i].HasObjects())
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool BoundsIntersects(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        return (
           aMax.x >= bMin.x
        && aMax.y >= bMin.y
        && aMax.z >= bMin.z
        && bMax.x >= aMin.x
        && bMax.y >= aMin.y
        && bMax.z >= aMin.z);
    }

    public static bool BoundsContains(Vector3 outerMin, Vector3 outerMax, Vector3 innerMin, Vector3 innerMax)
    {
        if (outerMin.x <= innerMin.x && outerMin.y <= innerMin.y && outerMin.z <= innerMin.z)
        {
            return (outerMax.x >= innerMax.x && outerMax.y >= innerMax.y && outerMax.z >= innerMax.z);
        }
        return false;
    }

    /// <summary>
    /// Find the index of the child bounds that the point falls in
    /// </summary>
    int OctantIndex(Vector3 point)
    {
        return (point.x <= Origin.x ? 0 : 1)
             + (point.z <= Origin.z ? 0 : 2)
             + (point.y >= Origin.y ? 0 : 4);
    }

    public void Draw(int counter = 0)
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
        for (int i = 0; i < Objects.Count; i++)
        {
            Vector3 center = (Objects[i].BoundsMax + Objects[i].BoundsMin) / 2f;
            Vector3 size = Objects[i].BoundsMax - Objects[i].BoundsMin;
            Gizmos.DrawCube(center, size);
        }

        Gizmos.color = new Color(counter / 5f, 1f, counter / 5f);
        Gizmos.DrawWireCube(Origin, Vector3.one * SizeWithOverlap);

        if (Children != null)
        {
            counter++;
            for (int i = 0; i < 8; i++)
            {
                Children[i].Draw(counter);
            }
        }
    }
}
