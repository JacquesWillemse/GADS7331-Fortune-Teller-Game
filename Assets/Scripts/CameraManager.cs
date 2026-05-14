using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera tentCamera;
    [SerializeField] private Camera bookCamera;
    [SerializeField] private Camera cardCamera;
    [SerializeField] private Camera spiritCamera;
    [SerializeField] private Camera judgeCamera;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private Canvas panelBook;
    [SerializeField] private GameObject panelBookMain;
    [SerializeField] private Canvas panelMain;
    [SerializeField] private Canvas panelJudge;
    [SerializeField] private GameObject panelUIJudge;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTentCamera();
    }
    private void DisableAllCameras()
    {
        tentCamera.gameObject.SetActive(false);
        bookCamera.gameObject.SetActive(false);
        cardCamera.gameObject.SetActive(false);
        spiritCamera.gameObject.SetActive(false);
        judgeCamera.gameObject.SetActive(false);
        uiManager.HideCardPanel();
        panelBook.gameObject.SetActive(false);
        panelBookMain.gameObject.SetActive(false);
        panelJudge.gameObject.SetActive(false);
        panelUIJudge.SetActive(false);
    }

    public void ActivateTentCamera()
    {
        DisableAllCameras();
        tentCamera.gameObject.SetActive(true);

    }

    public void ActivateBookCamera()
    {
        DisableAllCameras();
        bookCamera.gameObject.SetActive(true);
        panelBook.gameObject.SetActive(true);
        panelBookMain.gameObject.SetActive(true);
    }

    public void ActivateCardCamera()
    {
        DisableAllCameras();
        cardCamera.gameObject.SetActive(true);
        uiManager.ShowCardPanel();
    }

    public void ActivateSpiritCamera()
    {
        DisableAllCameras();
        spiritCamera.gameObject.SetActive(true);
    }

    public void ActivateJudgeCamera()
    {
        DisableAllCameras();
        judgeCamera.gameObject.SetActive(true);
        panelJudge.gameObject.SetActive(true);
        panelUIJudge.SetActive(true);
    }
}
