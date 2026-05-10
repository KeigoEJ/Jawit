using UnityEngine;
using UnityEngine.Events;

public class AvatarModelController : MonoBehaviour
{
    [Header("PlayerPrefs Key")]
    public string avatarKey = "AVATAR";
    
    [Header("Start Events")]
    public UnityEvent StartEvent;

    [Header("Avatar Array")]
    public int currentIndex = 0;
    public GameObject[] models;

    void Start()
    {
        // Load dari PlayerPrefs jika ada
        LoadAvatarPrefs();

        // Aktifkan avatar sesuai index terakhir
        SetActiveModel(currentIndex);

        // Invoke Unity Event
        StartEvent?.Invoke();
    }

    public void SetActiveModel(int index)
    {
        if (models == null || models.Length == 0) return;
        if (index < 0 || index >= models.Length) return;

        currentIndex = index;
        for (int i = 0; i < models.Length; i++)
        {
            models[i].SetActive(i == index);
        }
        AvatarSync();
    }

    public void AvatarSync()
    {
        if (models == null || models.Length == 0) return;
        Transform activeTransform = models[currentIndex].transform;

        for (int i = 0; i < models.Length; i++)
        {
            if (i == currentIndex) continue;
            Transform t = models[i].transform;
            t.position = activeTransform.position;
            t.rotation = activeTransform.rotation;
            t.localScale = activeTransform.localScale;
        }
    }

    void Update()
    {
        AvatarSync();
    }

    // ============================
    // PlayerPrefs Save / Load
    // ============================

    // Simpan index avatar aktif ke key default
    public void SaveAvatarIndex(int index)
    {
        currentIndex = index;
        PlayerPrefs.SetInt(avatarKey, currentIndex);
        PlayerPrefs.Save();
    }

    // Load index avatar dari key default
    public void LoadAvatarIndex()
    {
        if (PlayerPrefs.HasKey(avatarKey))
        {
            currentIndex = PlayerPrefs.GetInt(avatarKey, 0);
            SetActiveModel(currentIndex);
        }
    }

    // Simpan ke PlayerPrefs dengan key custom
    public void SaveAvatarPrefs(int index)
    {
        PlayerPrefs.SetInt(avatarKey, index);
        PlayerPrefs.Save();
    }

    // Load dari PlayerPrefs dengan key custom
    public void LoadAvatarPrefs()
    {
        if (PlayerPrefs.HasKey(avatarKey))
        {
            currentIndex = PlayerPrefs.GetInt(avatarKey, 0);
            SetActiveModel(currentIndex);
        }
    }
}
