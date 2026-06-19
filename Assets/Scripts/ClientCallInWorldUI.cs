using UnityEngine;

/// <summary>
/// World-space call-in prompt that floats above the client and faces the tent camera.
/// Fixes zero-scale / overlay canvases parented to characters.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class ClientCallInWorldUI : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] Camera eventCamera;
    [SerializeField] float worldScale = 0.01f;
    [SerializeField] Vector2 canvasSize = new Vector2(300f, 120f);

    Canvas _canvas;
    RectTransform _rect;

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

    void ApplyWorldSpaceSetup()
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        if (_canvas == null || _rect == null)
            return;

        _canvas.renderMode = RenderMode.WorldSpace;
        if (eventCamera == null)
            eventCamera = Camera.main;
        if (eventCamera != null)
            _canvas.worldCamera = eventCamera;

        if (_rect.localScale.sqrMagnitude < 1e-8f)
            _rect.localScale = Vector3.one * worldScale;

        _rect.sizeDelta = canvasSize;
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.localRotation = Quaternion.identity;

        for (int i = 0; i < _rect.childCount; i++)
        {
            if (_rect.GetChild(i) is RectTransform child)
            {
                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = Vector2.zero;
            }
        }
    }

    void LateUpdate()
    {
        if (followTarget == null)
            return;

        transform.position = followTarget.TransformPoint(localOffset);

        if (eventCamera == null)
            eventCamera = Camera.main;
        if (eventCamera == null)
            return;

        Vector3 toCamera = eventCamera.transform.position - transform.position;
        if (toCamera.sqrMagnitude < 1e-6f)
            return;

        transform.rotation = Quaternion.LookRotation(-toCamera.normalized, eventCamera.transform.up);
    }
}
