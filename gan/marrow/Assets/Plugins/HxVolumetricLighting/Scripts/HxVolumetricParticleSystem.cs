using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class HxVolumetricParticleSystem : MonoBehaviour
{
    public enum ParticleBlendMode {Max = 0, Add = 1, Min = 2, Sub = 3 };
    [Range(0,4f)]
    public float DensityStrength = 1f;
    HxOctreeNode<HxVolumetricParticleSystem>.NodeObject octreeNode = null;
    [HideInInspector]
    public Renderer particleRenderer;
    public ParticleBlendMode BlendMode = ParticleBlendMode.Add;
    Vector3 minBounds;
    Vector3 maxBounds;
    Bounds LastBounds;

    void OnEnable()
    {       
        particleRenderer = GetComponent<Renderer>();
        LastBounds = particleRenderer.bounds;
        minBounds = LastBounds.min;
        maxBounds = LastBounds.max;
        
        if (octreeNode == null)
        {
#if UNITY_EDITOR
            if(Application.isPlaying == false)
            GetComponent<ParticleSystem>().Simulate(0);
#endif
            HxVolumetricCamera.AllParticleSystems.Add(this);
            octreeNode = HxVolumetricCamera.AddParticleOctree(this, minBounds, maxBounds);
        }
    }


    public void UpdatePosition()
    {
        //probably just update every frame cause its a particle emitter?
        if (transform.hasChanged || true)
        {
            LastBounds = particleRenderer.bounds;
            minBounds = LastBounds.min;
            maxBounds = LastBounds.max;
            HxVolumetricCamera.ParticleOctree.Move(octreeNode, minBounds, maxBounds);
            transform.hasChanged = false;
        }
    }

    void OnDisable()
    {
        if (octreeNode != null)
        {
            HxVolumetricCamera.AllParticleSystems.Remove(this);
            HxVolumetricCamera.RemoveParticletOctree(this);
            octreeNode = null;
        }
    }

    void OnDestroy()
    {
        if (octreeNode != null)
        {
            HxVolumetricCamera.AllParticleSystems.Remove(this);
            HxVolumetricCamera.RemoveParticletOctree(this);
            octreeNode = null;
        }
    }
}
