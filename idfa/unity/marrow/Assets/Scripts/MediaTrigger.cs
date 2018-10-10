using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

public class MediaTrigger : MonoBehaviour {

	public event Action OnAllPlay;

	public enum State
    {
        Loading,
        Playing,
        Finished,
    }
    
    public MediaPlayer _masterPlayer;
    public MediaPlayer[] _slavePlayers;
	public Animator slaveAnimator;
	public Animation slaveAnimation;

    public float _toleranceMs = 30f;
    //public bool _matchVideo = true;
    //public bool _muteSlaves = true;
	public State _state = State.Loading;

	private AnimationClip slaveAnimationClip;
    
	void Start ()
	{
		if (slaveAnimation)
			slaveAnimationClip = slaveAnimation.clip;
	}
	
	void LateUpdate ()
	{
		if (_state == State.Loading)
        {
            // Finished loading?
            if (IsAllVideosLoaded())
            {
                // Play the videos
                _masterPlayer.Play();
                for (int i = 0; i < _slavePlayers.Length; i++)
                {
                    _slavePlayers[i].Play();
                }
				// Play the Animator
				//slaveAnimator.Play("main");
				//slaveAnimator.StartPlayback();
				slaveAnimation[slaveAnimationClip.name].weight = 1;
				slaveAnimation.Play();
				slaveAnimation[slaveAnimationClip.name].speed = 0;

                _state = State.Playing;

				if (OnAllPlay != null)
					OnAllPlay();
            }
        }
		else if (_state == State.Finished)
        {
            Debug.Log("Do Something");
        }
        else if (_state == State.Playing)
        {
            if (_masterPlayer.Control.IsPlaying())
            {
                // Keep the slaves synced
                float masterTime = _masterPlayer.Control.GetCurrentTimeMs();
                for (int i = 0; i < _slavePlayers.Length; i++)
                {
                    MediaPlayer slave = _slavePlayers[i];
                    float slaveTime = slave.Control.GetCurrentTimeMs();
                    float deltaTime = Mathf.Abs(masterTime - slaveTime);
                    if (deltaTime > _toleranceMs)
                    {
                        slave.Control.SeekFast(masterTime + (_toleranceMs * 0.5f)); // Add a bit to allow for the delay in playback start
                        if (slave.Control.IsPaused())
                        {
                            slave.Play();
                        }
                    }
                }

				// Try to keep the animation synced
				//slaveAnimationClip.SampleAnimation(slaveAnimator.gameObject, masterTime * 1000f);
				slaveAnimation[slaveAnimationClip.name].time = masterTime * 1000f;
				//slaveAnimator.playbackTime = masterTime * 1000f;
            }
            else
            {
                // Pause slaves
                for (int i = 0; i < _slavePlayers.Length; i++)
                {
                    MediaPlayer slave = _slavePlayers[i];
                    slave.Pause();
                }

				// Animator
				//slaveAnimator.StopPlayback();
				//slaveAnimator.StopPlayback();
				slaveAnimation.Stop();
            }

            // Finished?
            if (IsPlaybackFinished(_masterPlayer))
            {
                _state = State.Finished;
            }
        }
	}

	private bool IsAllVideosLoaded()
    {
        bool result = false;
        if (IsVideoLoaded(_masterPlayer))
        {
            result = true;
            for (int i = 0; i < _slavePlayers.Length; i++)
            {
                if (!IsVideoLoaded(_slavePlayers[i]))
                {
                    result = false;
                    break;
                }
            }
        }
        return result;
    }

    private static bool IsVideoLoaded(MediaPlayer player)
    {
        return (player != null && player.Control != null && player.Control.HasMetaData() && player.Control.CanPlay() && player.TextureProducer.GetTextureFrameCount() > 0);
    }

    private static bool IsPlaybackFinished(MediaPlayer player)
    {
        bool result = false;
        if (player != null && player.Control != null)
        {
            if (player.Control.IsFinished())
            {
                result = true;
            }
        }
        return result;
    }
}
