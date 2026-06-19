using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Spawns a client at the door, rolls wealth + props, and moves them to the seat when <see cref="CallClientIn"/> runs.
/// Wire the call-in button to <see cref="CallClientIn"/>; gameplay stays locked until the client reaches the seat.
/// </summary>
public class FortuneClientSpawner : MonoBehaviour
{
    public enum WealthType
    {
        Poor,
        Rich
    }

    public enum ClientProp
    {
        RichMonocle,
        RichSuit,
        RichTopHat,
        PoorBarrel,
        PoorBrokenHat,
        PoorGlasses
    }

    [Header("Client")]
    [Tooltip("Client character root (e.g. Char.fbx instance). Enabled when SpawnClient runs.")]
    [SerializeField] private GameObject clientCharacter;
    [Tooltip("Base body mesh (child of client). Always enabled when the client spawns. Props stay separate.")]
    [SerializeField] private GameObject clientBody;

    [Header("Rich props (enable 1–3 when wealth is Rich)")]
    [SerializeField] private GameObject richMonocle;
    [SerializeField] private GameObject richSuit;
    [SerializeField] private GameObject richTopHat;

    [Header("Poor props (enable 1–3 when wealth is Poor)")]
    [SerializeField] private GameObject poorBarrel;
    [SerializeField] private GameObject poorBrokenHat;
    [SerializeField] private GameObject poorGlasses;

    [Header("Positions")]
    [Tooltip("Empty at the tent door — client is placed here on spawn.")]
    [SerializeField] private Transform doorWaypoint;
    [Tooltip("Empty at the table — client walks here when called in.")]
    [SerializeField] private Transform seatWaypoint;
    [SerializeField] private float moveDuration = 1.5f;

    [Header("Call in")]
    [Tooltip("Root of the CharCall world canvas (optional — auto-found from Call In Button).")]
    [SerializeField] private GameObject callInPromptRoot;
    [Tooltip("Empty above the client's head. If empty, uses Client Character transform + head offset.")]
    [SerializeField] private Transform callInHeadAnchor;
    [Tooltip("Tent camera for world UI raycasts and billboarding. Falls back to Camera.main.")]
    [SerializeField] private Camera callInEventCamera;
    [SerializeField] private Vector3 callInHeadOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float callInWorldCanvasScale = 0.01f;
    [Tooltip("Optional — disabled after the client is seated until the next SpawnClient.")]
    [SerializeField] private Button callInButton;

    ClientCallInWorldUI _callInWorldUi;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    [Tooltip("Chance that the next client is Rich; otherwise Poor.")]
    [SerializeField, Range(0f, 1f)] private float richProbability = 0.5f;

    [Header("Events")]
    public UnityEvent<WealthType> onClientSpawned;
    public UnityEvent onClientCalledIn;

    static readonly ClientProp[] RichProps =
    {
        ClientProp.RichMonocle,
        ClientProp.RichSuit,
        ClientProp.RichTopHat
    };

    static readonly ClientProp[] PoorProps =
    {
        ClientProp.PoorBarrel,
        ClientProp.PoorBrokenHat,
        ClientProp.PoorGlasses
    };

    readonly List<ClientProp> _activeProps = new List<ClientProp>(3);

    WealthType _wealth = WealthType.Poor;
    bool _clientCalledIn;
    Coroutine _moveRoutine;

    public WealthType CurrentWealth => _wealth;
    public bool IsRich => _wealth == WealthType.Rich;
    public bool IsClientCalledIn => _clientCalledIn;
    public bool IsClientMoving => _moveRoutine != null;
    public bool IsGameplayAllowed => _clientCalledIn && !IsClientMoving;
    public IReadOnlyList<ClientProp> ActiveProps => _activeProps;
    public GameObject ClientCharacter => clientCharacter;

    void Awake()
    {
        DisableAllProps();
        ResolveCallInReferences();
        if (callInButton != null)
            callInButton.onClick.AddListener(CallClientIn);
    }

    void OnDestroy()
    {
        if (callInButton != null)
            callInButton.onClick.RemoveListener(CallClientIn);
    }

    void Start()
    {
        if (spawnOnStart)
            SpawnClient();
    }

    /// <summary>Wire to the Call Client In button.</summary>
    public void CallClientIn()
    {
        if (_clientCalledIn || IsClientMoving)
            return;
        if (clientCharacter == null)
        {
            Debug.LogWarning("[FortuneClientSpawner] Assign Client Character.", this);
            return;
        }
        if (seatWaypoint == null)
        {
            Debug.LogWarning("[FortuneClientSpawner] Assign Seat Waypoint.", this);
            return;
        }

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);
        SetCallInPromptVisible(false);
        _moveRoutine = StartCoroutine(MoveClientToSeat());
        RefreshCallInButton();
    }

    /// <summary>Pick wealth, place at door, enable 1–3 props. Resets call-in state.</summary>
    public void SpawnClient()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _clientCalledIn = false;
        SetClientVisible(true);
        DisableAllProps();
        _activeProps.Clear();

        _wealth = Random.value < richProbability ? WealthType.Rich : WealthType.Poor;
        ClientProp[] pool = _wealth == WealthType.Rich ? RichProps : PoorProps;

        int propCount = Random.Range(1, pool.Length + 1);
        PickAndEnableProps(pool, propCount);

        PlaceClientAt(doorWaypoint);
        SetCallInPromptVisible(true);
        RefreshCallInButton();
        onClientSpawned?.Invoke(_wealth);
    }

    IEnumerator MoveClientToSeat()
    {
        Transform t = clientCharacter.transform;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        Vector3 endPos = seatWaypoint.position;
        Quaternion endRot = seatWaypoint.rotation;
        float duration = Mathf.Max(0.05f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.SetPositionAndRotation(
                Vector3.Lerp(startPos, endPos, u),
                Quaternion.Slerp(startRot, endRot, u));
            yield return null;
        }

        t.SetPositionAndRotation(endPos, endRot);
        _moveRoutine = null;
        _clientCalledIn = true;
        SetCallInPromptVisible(false);
        RefreshCallInButton();
        onClientCalledIn?.Invoke();
    }

    void PlaceClientAt(Transform waypoint)
    {
        if (clientCharacter == null || waypoint == null)
            return;
        clientCharacter.transform.SetPositionAndRotation(waypoint.position, waypoint.rotation);
    }

    void PickAndEnableProps(ClientProp[] pool, int count)
    {
        var indices = new int[pool.Length];
        for (int i = 0; i < pool.Length; i++)
            indices[i] = i;

        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(i, pool.Length);
            (indices[i], indices[pick]) = (indices[pick], indices[i]);

            ClientProp prop = pool[indices[i]];
            if (TryEnableProp(prop))
                _activeProps.Add(prop);
        }
    }

    bool TryEnableProp(ClientProp prop)
    {
        GameObject go = GetPropObject(prop);
        if (go == null)
        {
            Debug.LogWarning($"[FortuneClientSpawner] Missing prop object for {prop}.", this);
            return false;
        }

        go.SetActive(true);
        return true;
    }

    GameObject GetPropObject(ClientProp prop)
    {
        switch (prop)
        {
            case ClientProp.RichMonocle: return richMonocle;
            case ClientProp.RichSuit: return richSuit;
            case ClientProp.RichTopHat: return richTopHat;
            case ClientProp.PoorBarrel: return poorBarrel;
            case ClientProp.PoorBrokenHat: return poorBrokenHat;
            case ClientProp.PoorGlasses: return poorGlasses;
            default: return null;
        }
    }

    void DisableAllProps()
    {
        SetPropActive(richMonocle, false);
        SetPropActive(richSuit, false);
        SetPropActive(richTopHat, false);
        SetPropActive(poorBarrel, false);
        SetPropActive(poorBrokenHat, false);
        SetPropActive(poorGlasses, false);
    }

    void SetClientVisible(bool visible)
    {
        if (clientCharacter != null)
            clientCharacter.SetActive(visible);

        if (clientBody != null)
            clientBody.SetActive(visible);
        else if (visible && clientCharacter != null)
            TryEnableDefaultBody(clientCharacter.transform);
    }

    void TryEnableDefaultBody(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (IsKnownProp(child.gameObject))
                continue;
            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                child.gameObject.SetActive(true);
                return;
            }
        }
    }

    bool IsKnownProp(GameObject go)
    {
        return go == richMonocle || go == richSuit || go == richTopHat
            || go == poorBarrel || go == poorBrokenHat || go == poorGlasses;
    }

    void RefreshCallInButton()
    {
        if (callInButton == null)
            return;
        bool promptVisible = callInPromptRoot == null || callInPromptRoot.activeSelf;
        callInButton.interactable = promptVisible && !_clientCalledIn && !IsClientMoving && clientCharacter != null;
    }

    void ResolveCallInReferences()
    {
        if (callInButton != null && callInPromptRoot == null)
        {
            Transform node = callInButton.transform;
            while (node != null)
            {
                if (node.GetComponent<Canvas>() != null)
                {
                    callInPromptRoot = node.gameObject;
                    break;
                }
                node = node.parent;
            }
        }
    }

    void EnsureCallInWorldUi()
    {
        if (callInPromptRoot == null)
            return;

        if (_callInWorldUi == null)
            _callInWorldUi = callInPromptRoot.GetComponent<ClientCallInWorldUI>();
        if (_callInWorldUi == null)
            _callInWorldUi = callInPromptRoot.AddComponent<ClientCallInWorldUI>();

        Transform follow = callInHeadAnchor != null
            ? callInHeadAnchor
            : clientCharacter != null ? clientCharacter.transform : null;

        Camera cam = callInEventCamera != null ? callInEventCamera : Camera.main;
        _callInWorldUi.Configure(follow, cam, callInHeadOffset, callInWorldCanvasScale);
    }

    void SetCallInPromptVisible(bool visible)
    {
        if (callInPromptRoot != null)
            callInPromptRoot.SetActive(visible);

        if (visible)
            EnsureCallInWorldUi();

        RefreshCallInButton();
    }

    static void SetPropActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

#if UNITY_EDITOR
    [ContextMenu("Spawn Client (editor test)")]
    void EditorSpawnClient()
    {
        SpawnClient();
    }
#endif
}
