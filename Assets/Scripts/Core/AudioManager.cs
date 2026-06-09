using UnityEngine;
using System.Collections;

/// <summary>
/// 音频管理器 — BGM + 音效播放，音量跟随 GameManager.volume
/// 音频文件放 Assets/StreamingAssets/Audio/ 目录下
/// BGM: bgm.mp3 / bgm.wav
/// SFX: click.wav, flip.wav, collect.wav, toast.wav
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[AudioManager]");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioClip bgmClip;
    private AudioClip clickClip;
    private AudioClip flipClip;
    private AudioClip collectClip;
    private AudioClip toastClip;
    private bool initialized;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 清理场景中其他 AudioListener（如 Main Camera 上的），确保唯一
        CleanupExtraListeners();
        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        // 场景切换后自动清理新场景中 Camera 携带的 AudioListener
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        StartCoroutine(LoadAudioAsync());
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CleanupExtraListeners();
    }

    private void CleanupExtraListeners()
    {
        var listeners = FindObjectsOfType<AudioListener>();
        foreach (var listener in listeners)
        {
            if (listener.gameObject != gameObject)
                Destroy(listener);
        }
    }

    private IEnumerator LoadAudioAsync()
    {
        string basePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Audio");

        // 加载 BGM
        yield return LoadClip(basePath, "bgm", clip => bgmClip = clip);
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            UpdateVolume();
            bgmSource.Play();
        }

        // 加载 SFX
        yield return LoadClip(basePath, "click", clip => clickClip = clip);
        yield return LoadClip(basePath, "flip", clip => flipClip = clip);
        yield return LoadClip(basePath, "collect", clip => collectClip = clip);
        yield return LoadClip(basePath, "toast", clip => toastClip = clip);

        initialized = true;
    }

    private IEnumerator LoadClip(string dir, string name, System.Action<AudioClip> onLoaded)
    {
        // 尝试 .wav 和 .mp3
        string[] exts = { ".wav", ".mp3" };
        foreach (var ext in exts)
        {
            string path = System.IO.Path.Combine(dir, name + ext);
            if (!System.IO.File.Exists(path)) continue;

            string url = "file:///" + path.Replace("\\", "/");
            using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                // WAV 从 StreamingAssets 加载需关闭流式模式，否则 clip 可能为空/无声
                var dh = www.downloadHandler as UnityEngine.Networking.DownloadHandlerAudioClip;
                if (dh != null) dh.streamAudio = false;
                yield return www.SendWebRequest();
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null && clip.loadState == AudioDataLoadState.Loaded)
                    {
                        clip.name = name;
                        onLoaded(clip);
                        Debug.Log($"[Audio] Loaded: {name}{ext} ({clip.length:F1}s)");
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"[Audio] {name}{ext} clip not loaded properly (state={clip?.loadState})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Audio] Failed to load {name}{ext}: {www.error}");
                }
            }
        }
        Debug.LogWarning($"[Audio] File not found: {dir}/{name}");
    }

    // ──────────────────── 公共接口 ────────────────────

    public void PlayBGM()
    {
        if (bgmClip != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlayClick()
    {
        PlaySFX(clickClip);
    }

    public void PlayFlip()
    {
        PlaySFX(flipClip);
    }

    public void PlayCollect()
    {
        PlaySFX(collectClip);
    }

    public void PlayToast()
    {
        PlaySFX(toastClip);
    }

    public void UpdateVolume()
    {
        float vol = GameManager.Instance != null ? GameManager.Instance.volume : 1f;
        bgmSource.volume = vol * 0.5f;    // BGM 略低
        sfxSource.volume = vol;
    }

    // ──────────────────── 内部 ────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && initialized)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
