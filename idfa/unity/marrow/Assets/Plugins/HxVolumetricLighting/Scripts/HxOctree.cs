using System.Collections.Generic;
using UnityEngine;

public class HxOctree<T>
{
    public int Count { get; private set; }

    HxOctreeNode<T> Root;
    float Overlap;
    float InitialSize;
    float MinNodeSize;

    Dictionary<T, HxOctreeNode<T>.NodeObject> NodeMap;

    /// <summary>
    /// Create a bounds octree.
    /// </summary>
    /// <param name="origin">Position of the centre of the initial node.</param>
    /// <param name="initialSize">Starting node size.</param>
    /// <param name="overlap">Percentage overlap between nodes (0-1).</param>
    /// <param name="minNodeSize">Cannot split past this node size.</param>
    public HxOctree(Vector3 origin = new Vector3(), float initialSize = 10f, float overlap = 0f, float minNodeSize = 1f)
    {
        Count = 0;
        InitialSize = Mathf.Max(minNodeSize, initialSize);
        MinNodeSize = Mathf.Min(minNodeSize, initialSize);
        Overlap = Mathf.Clamp(overlap, 0f, 1f);
        Root = new HxOctreeNode<T>(InitialSize, overlap, MinNodeSize, origin, null);
        NodeMap = new Dictionary<T, HxOctreeNode<T>.NodeObject>();
    }

    public HxOctreeNode<T>.NodeObject Add(T value, Vector3 boundsMin, Vector3 boundsMax)
    {
        int counter = 0;
        while (!HxOctreeNode<T>.BoundsContains(Root.BoundsMin, Root.BoundsMax, boundsMin, boundsMax))
        {
            ExpandRoot((boundsMin + boundsMax) / 2f);
            if (++counter > 16)
            {
                Debug.LogError("The octree could not contain the bounds.");
                return null;
            }
        }

        var node = new HxOctreeNode<T>.NodeObject(value, boundsMin, boundsMax);
        NodeMap[value] = node;
        Root.Add(node);
        Count++;
        return node;
    }

    public void Print()
    {
        Debug.Log("=============================");
        string type = "";
        foreach (var pair in NodeMap)
        {
            if (pair.Value.Node.Children == null)
            {
                type = "leaf";
            }
            else
            {
                type = "branch";
            }
            Debug.Log(pair.Key + " is in " + pair.Value.Node.ID + ", a " + type + ".");
        }
    }

    public void Move(HxOctreeNode<T>.NodeObject value, Vector3 boundsMin, Vector3 boundsMax)
    {
        if (value == null) { Debug.Log("null"); }
        value.BoundsMin = boundsMin;
        value.BoundsMax = boundsMax;
        var currentNode = value.Node;
        if (!HxOctreeNode<T>.BoundsContains(currentNode.BoundsMin, currentNode.BoundsMax, boundsMin, boundsMax))
        {
            currentNode.Remove(value.Value);
            int counter = 0;
            while (!HxOctreeNode<T>.BoundsContains(currentNode.BoundsMin, currentNode.BoundsMax, boundsMin, boundsMax))
            {
                if (currentNode.Parent != null)
                {
                    currentNode = currentNode.Parent;
                }
                else // current node is the root, expand it
                {
                    counter++;
                    ExpandRoot((boundsMin + boundsMax) / 2f);
                    currentNode = Root;
                    if (counter > 16)
                    {
                        Debug.LogError("The octree could not contain the bounds.");
                        return;
                    }
                }
            }
            currentNode.Add(value);
        }
        //   Root = Root.TryShrink(InitialSize);
    }

    public void Move(T value, Vector3 boundsMin, Vector3 boundsMax)
    {
        HxOctreeNode<T>.NodeObject obj;
        if (NodeMap.TryGetValue(value, out obj))
        {
            Move(obj, boundsMin, boundsMax);
        }
    }

    public void TryShrink()
    {
        Root = Root.TryShrink(InitialSize);
    }

    /// <summary>
    /// Remove an object and try to shrink the octree.
    /// </summary>
    public bool Remove(T value)
    {
        if (Root.Remove(value))
        {
            NodeMap.Remove(value);
            Count--;
            Root = Root.TryShrink(InitialSize);
            return true;
        }
        return false;
    }

    void ExpandRoot(Vector3 center)
    {
        Vector3 direction = Root.Origin - center;

        int xDir = direction.x < 0 ? -1 : 1;
        int yDir = direction.y < 0 ? -1 : 1;
        int zDir = direction.z < 0 ? -1 : 1;

        HxOctreeNode<T> oldRoot = Root;
        float halfSize = Root.Size / 2f;
        Vector3 newOrigin = Root.Origin - new Vector3(xDir, yDir, zDir) * halfSize;

        // Expand the root node to a new one that includes the old one as a child
        Root = new HxOctreeNode<T>(Root.Size * 2f, Overlap, MinNodeSize, newOrigin, null);
        oldRoot.Parent = Root;

        int index = 0;
        if (xDir > 0) index += 1;
        if (zDir > 0) index += 2;
        if (yDir < 0) index += 4;

        HxOctreeNode<T>[] children = new HxOctreeNode<T>[8];
        for (int i = 0; i < 8; i++)
        {
            if (i == index)
            {
                children[i] = oldRoot;
            }
            else
            {
                xDir = (i % 2 == 0) ? -1 : 1;
                yDir = (i > 3) ? -1 : 1;
                zDir = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;
                children[i] = new HxOctreeNode<T>(oldRoot.Size, Overlap, MinNodeSize, newOrigin + new Vector3(xDir, yDir, zDir) * halfSize, Root);
            }
        }
        Root.Children = children;
    }

    /// <summary>
    /// Get all the objects within the bounds.
    /// </summary>
    public void GetObjects(Vector3 boundsMin, Vector3 boundsMax, List<T> items)
    {
        Root.GetObjects2(boundsMin, boundsMax, items);
    }
    public void GetObjectsBoundsPlane(ref Plane[] planes,Vector3 min,  Vector3 max, List<T> items)
    {
        Root.GetObjects2BoundsPlane(ref planes, min, max, items);
    }
    

    /// <summary>
    /// Draw a gizmo representing the octree.
    /// </summary>
    public void Draw()
    {
        Root.Draw();
    }
}
