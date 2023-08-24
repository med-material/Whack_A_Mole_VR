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

    public enum Sound
    {
        greenMoleHit,
        neutralMoleHit,
        redMoleHit,
        missedMole,
        countdown,
        laserInMotorSpace,
        laserOutMotorSpace,
        outOfBoundClick,
        trailPointerMoving
    }

    public static SoundManager Instance = null;

    // A dictionary to keep track of the looping sounds that are currently playing.
    private Dictionary<Sound, AudioSource> loopingSounds = new Dictionary<Sound, AudioSource>();

    [SerializeField]
    internal const float FADE_SPEED = 0.5f;

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

    // whenever you want to reference a sound, put this line in the spot where you want it to play:
    // SoundManager.PlaySound(SoundManager.Sound.SoundName);

    private AudioClip GetAudioClip(Sound sound) // creating a function that will cycle through the array until it reaches the                                                    
    {                                           // corresponding sound object that it's looking for - else return null and msg
        foreach (SoundAudioClip soundAudioClip in soundAudioClipArray)
        {
            if (soundAudioClip.sound == sound)
            {
                return soundAudioClip.audio;
            }
        }
        Debug.LogError("Sound " + sound + " not found!");
        return null;
    }

    public void PlaySound(GameObject source, Sound sound)
    {
        AudioSource _audioSource = source.GetComponent<AudioSource>();

        if (source == null)
        {
            SoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(GetAudioClip(sound));

            return;
        }

        if (_audioSource == null)
        {
            source.AddComponent<AudioSource>();
            _audioSource = source.GetComponent<AudioSource>();
        }

        if (_audioSource)
        {
            _audioSource.pitch = 1f; // reset pitch to 1f in case it was changed by PlaySoundWithPitch
            _audioSource.PlayOneShot(GetAudioClip(sound));
        }
    }

    public void PlaySoundWithPitch(GameObject source, Sound sound, float feedback)
    {
        float pitchValue = (feedback * 0.47f) + 0.7f;
        AudioSource _audioSource = source.GetComponent<AudioSource>();
        if (source == null)
        {
            AudioSource instanceAudioSource = SoundManager.Instance.GetComponent<AudioSource>();
            instanceAudioSource.pitch = pitchValue;
            instanceAudioSource.PlayOneShot(GetAudioClip(sound));
            return;
        }
        if (_audioSource == null)
        {
            source.AddComponent<AudioSource>();
            _audioSource = source.GetComponent<AudioSource>();
        }
        if (_audioSource)
        {
            {
                _audioSource.pitch = pitchValue;
                _audioSource.PlayOneShot(GetAudioClip(sound));
                return;
            }
        }
    }

    public void PlaySoundWithPitch(GameObject source, Sound sound, float value, float pitch = 0.47f, float flag = 0.7f)
    {
        float pitchValue = (value * pitch) + flag;
        AudioSource _audioSource = source.GetComponent<AudioSource>();
        if (source == null)
        {
            AudioSource instanceAudioSource = SoundManager.Instance.GetComponent<AudioSource>();
            instanceAudioSource.pitch = pitchValue;
            instanceAudioSource.PlayOneShot(GetAudioClip(sound));
            return;
        }
        if (_audioSource == null)
        {
            source.AddComponent<AudioSource>();
            _audioSource = source.GetComponent<AudioSource>();
        }
        if (_audioSource)
        {
            {
                _audioSource.pitch = pitchValue;
                _audioSource.PlayOneShot(GetAudioClip(sound));
                return;
            }
        }
    }

    

    // Play a sound in a loop
    public void PlaySoundLooped(GameObject source, Sound sound)
    {
        AudioSource _audioSource = source.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            source.AddComponent<AudioSource>();
            _audioSource = source.GetComponent<AudioSource>();
        }
        _audioSource.loop = true;
        _audioSource.clip = GetAudioClip(sound);
        _audioSource.Play();

        // Store the AudioSource in the dictionary so we can modify or stop it later.
        loopingSounds[sound] = _audioSource;
    }

    // Change the pitch of a currently playing, looping sound
    public void ChangePitch(Sound sound, float pitch)
    {
        // If the sound is currently playing
        if (loopingSounds.ContainsKey(sound))
        {
            loopingSounds[sound].pitch = pitch;
        }
        else
        {
            Debug.LogError("Sound " + sound + " is not currently playing!");
        }
    }

    // Change the volume of a currently playing, looping sound
    public void ChangeVolume(Sound sound, float volume)
    {
        // If the sound is currently playing
        if (loopingSounds.ContainsKey(sound))
        {
            loopingSounds[sound].volume = volume;
        }
        else
        {
            Debug.LogError("Sound " + sound + " is not currently playing!");
        }
    }

    // Stop a currently playing, looping sound
    public void StopSound(Sound sound)
    {
        // If the sound is currently playing
        if (loopingSounds.ContainsKey(sound))
        {
            loopingSounds[sound].Stop();
            loopingSounds.Remove(sound);
        }
        //else
        //{
        //    Debug.LogError("Sound " + sound + " is not currently playing!");
        //}
    }



}


