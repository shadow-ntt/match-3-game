using UnityEngine;
using UnityEngine.Audio;

// Dinh nghia du lieu va hanh vi cua 1 su kien am thanh duoi dang ScriptableObject
[CreateAssetMenu(fileName = "NewAudioEvent", menuName = "Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private AudioMixerGroup mixerGroup;
    [SerializeField] private float cooldown = 0.04f;

    private float lastPlayTime = -999f;

    public AudioClip[] Clips => clips;
    public float Volume => volume;
    public AudioMixerGroup MixerGroup => mixerGroup;

    // Kiem tra cooldown de tranh phat lap am thanh qua nhieu lan trong 1 khung hinh (tu dong reset neu thoi gian bi quay lui khi restart)
    public bool CanPlay => Time.unscaledTime < lastPlayTime || Time.unscaledTime - lastPlayTime >= cooldown;

    // Phat 1 clip ngau nhien voi volume va pitch ngau nhien qua AudioSource duoc chi dinh
    public void Play(AudioSource source, float pitchMultiplier = 1f)
    {
        if (clips.Length == 0 || !CanPlay) return;

        lastPlayTime = Time.unscaledTime;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float basePitch = Random.Range(pitchRange.x, pitchRange.y);

        source.pitch = basePitch * pitchMultiplier;
        source.outputAudioMixerGroup = mixerGroup;
        source.PlayOneShot(clip, volume);
    }

    // Phat am thanh lap lai (Loop) danh cho nhac nen BGM
    public void PlayLoop(AudioSource source)
    {
        if (clips.Length == 0) return;

        source.clip = clips[0];
        source.loop = true;
        source.volume = volume;
        source.pitch = 1f;
        source.outputAudioMixerGroup = mixerGroup;
        source.Play();
    }
}
