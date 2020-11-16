using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SoundAudioClip
{
    public string name;
    public SoundManager.Sound sound;
    public AudioClip audio;
}

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    public SoundAudioClip[] soundAudioClipArray;

    public enum Sound {
        greenMoleHit, 
        neutralMoleHit,
        redMoleHit,
        missedMole,
        countdown,
        laserInMotorSpace,
        laserOutMotorSpace,
        outOfBoundClick
    }


    public static SoundManager Instance = null;
    private void Awake()
    {
        // If there isn't already an instance of the SoundManager, set it to this. 
        if (Instance == null)
        {
            Instance = this;
        } 
        // If there is an existing instance, destroy it. 
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        //Setting dontdestroyonload to our soundmanager so it will keep being there when reloading the scene.
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySound(GameObject source, Sound sound)
    {
        if(source == null)
        {
            SoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(GetAudioClip(sound));

            return;
        }

        AudioSource _audioSource = source.GetComponent<AudioSource>();

        if (_audioSource == null)
        {
            source.AddComponent<AudioSource>();
        }

        _audioSource.PlayOneShot(GetAudioClip(sound)); 
    }

    // whenever you want to reference a sound, put this line in the spot where you want it to play:
    // SoundManager.PlaySound(SoundManager.Sound.SoundName);

    private AudioClip GetAudioClip(Sound sound) // creating a function that will cycle through the array until it reaches the                                                    
    {                                           // corresponding sound object that it's looking for - else return null and msg
        foreach (SoundAudioClip soundAudioClip in soundAudioClipArray)
        {
            if(soundAudioClip.sound == sound)
            {
                return soundAudioClip.audio;
            }
        }
        Debug.LogError("Sound " + sound + " not found!");
        return null;
    }
}


