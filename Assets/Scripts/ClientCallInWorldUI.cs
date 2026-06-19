using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space call-in prompt that floats above the client and faces the active camera.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class ClientCallInWorldUI : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] Camera eventCamera;
    [SerializeField] float worldScale = 0.0125f;
    [SerializeField] Vector2 canvasSize = new Vector2(400f, 160f);
    [SerializeField] int sortingOrder = 200;

    Canvas _canvas;
    RectTransform _rect;
    CanvasScaler _scaler;

    public void Configure(Transform target, Camera camera, Vector3 offset, float scale)
    {
        followTarget = target;
        eventCamera = camera;
        localOffset = offset;
        worldScale = scale;
        ApplyWorldSpaceSetup();
    }

    void Awake()
    {
        ApplyWorldSpaceSetup();
    }

    void OnEnable()
    {
        ApplyWorldSpaceSetup();
    }

    public void ApplyWorldSpaceSetup()
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        if (_scaler == null)
            _scaler = GetComponent<CanvasScaler>();

        if (_canvas == null || _rect == null)
            return;

        if (_scaler != null)
            _scaler.enabled = false;

        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = sortingOrder;

        Camera cam = ResolveEventCamera();
        if (cam != null)
            _canvas.worldCamera = cam;

        _rect.localScale = Vector3.one * Mathf.Max(0.001f, worldScale);
        _rect.sizeDelta = canvasSize;
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.localPosition = Vector3.zero;
        _rect.localRotation = Quaternion.identity;

        NormalizeChildRects(_rect);
    }

    static void NormalizeChildRects(RectTransform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i) is not RectTransform child)
                continue;

            child.localScale = Vector3.one;
            child.localRotation = Quaternion.identity;
            child.localPosition = Vector3.zero;
            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = Vector2.zero;
            if (child.sizeDelta.sqrMagnitude < 1f)
                child.sizeDelta = new Vector2(120f, 120f);

            NormalizeChildRects(child);
        }
    }

    Camera ResolveEventCamera()
    {
        if (eventCamera != null && eventCamera.isActiveAndEnabled)
            return eventCamera;

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                return cameras[i];
        }

        return null;
    }

    void LateUpdate()
    {
        if (followTarget == null)
            return;

        transform.position = followTarget.TransformPoint(localOffset);

        Camera cam = ResolveEventCamera();
        if (cam == null)
            return;

        if (_canvas != null && _canvas.worldCamera != cam)
            _canvas.worldCamera = cam;

        Vector3 toCamera = cam.transform.position - transform.position;
        if (toCamera.sqrMagnitude < 1e-6f)
            return;

        transform.rotation = Quaternion.LookRotation(-toCamera.normalized, cam.transform.up);
    }
}
