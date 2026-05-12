using UnityEngine;

/// <summary>
/// Enables one of several fixed <see cref="Camera"/> rigs at a time. Wire UI buttons to
/// <see cref="SwitchTo0"/> … <see cref="SwitchTo7"/> or call <see cref="SwitchTo"/> from code.
/// Keeps a single active <see cref="AudioListener"/> on the active camera when possible.
/// </summary>
public class FixedCameraSwitcher : MonoBehaviour
{
    [Tooltip("Order matters: index 0 is first shot, 1 second, etc.")]
    [SerializeField] Camera[] cameras;

    [Tooltip("Which camera is active on Play.")]
    [SerializeField] int startIndex;

    [Tooltip("If true, only the active camera GameObject stays active (saves culling). If false, only Camera.enabled toggles.")]
    [SerializeField] bool deactivateWholeRig = true;

    int _current = -1;

    void Awake()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogWarning($"{nameof(FixedCameraSwitcher)} on {name}: assign at least one Camera.", this);
            return;
        }

        int start = Mathf.Clamp(startIndex, 0, cameras.Length - 1);
        Apply(start);
    }

    /// <summary>Switch by slot index (0-based).</summary>
    public void SwitchTo(int index)
    {
        if (cameras == null || cameras.Length == 0)
            return;
        Apply(Mathf.Clamp(index, 0, cameras.Length - 1));
    }

    public void SwitchTo0() => SwitchTo(0);
    public void SwitchTo1() => SwitchTo(1);
    public void SwitchTo2() => SwitchTo(2);
    public void SwitchTo3() => SwitchTo(3);
    public void SwitchTo4() => SwitchTo(4);
    public void SwitchTo5() => SwitchTo(5);
    public void SwitchTo6() => SwitchTo(6);
    public void SwitchTo7() => SwitchTo(7);

    public void SwitchNext()
    {
        if (cameras == null || cameras.Length == 0)
            return;
        int n = (_current + 1) % cameras.Length;
        Apply(n);
    }

    public void SwitchPrevious()
    {
        if (cameras == null || cameras.Length == 0)
            return;
        int n = (_current - 1 + cameras.Length) % cameras.Length;
        Apply(n);
    }

    void Apply(int index)
    {
        _current = index;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
                continue;

            bool on = i == index;
            if (deactivateWholeRig)
                cam.gameObject.SetActive(on);
            else
                cam.enabled = on;

            if (on)
                TagMainCameraIfNeeded(cam);
        }

        EnsureSingleAudioListener();
    }

    static void TagMainCameraIfNeeded(Camera active)
    {
        if (active == null || !active.gameObject.activeInHierarchy || !active.enabled)
            return;

        Camera[] all = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Camera c in all)
        {
            if (c != null && c.CompareTag("MainCamera") && c != active)
                c.tag = "Untagged";
        }

        if (!active.CompareTag("MainCamera"))
            active.tag = "MainCamera";
    }

    void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera activeCam = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].gameObject.activeInHierarchy && cameras[i].enabled)
            {
                activeCam = cameras[i];
                break;
            }
        }

        if (activeCam == null)
        {
            foreach (AudioListener l in listeners)
                if (l != null)
                    l.enabled = false;
            return;
        }

        AudioListener onActive = activeCam.GetComponent<AudioListener>();
        if (onActive == null)
            onActive = activeCam.gameObject.AddComponent<AudioListener>();

        foreach (AudioListener l in listeners)
        {
            if (l == null)
                continue;
            l.enabled = l == onActive;
        }
    }
}
