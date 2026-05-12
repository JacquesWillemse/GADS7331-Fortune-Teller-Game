using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera tentCamera;
    [SerializeField] private Camera bookCamera;
    [SerializeField] private Camera cardCamera;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private Canvas panelBook;
    [SerializeField] private Canvas panelMain;

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
        uiManager.HideCardPanel();
        panelBook.gameObject.SetActive(false);
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
    }

    public void ActivateCardCamera()
    {
        DisableAllCameras();
        cardCamera.gameObject.SetActive(true);
        uiManager.ShowCardPanel();
    }
}
