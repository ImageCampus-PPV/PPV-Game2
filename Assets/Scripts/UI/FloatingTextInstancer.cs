using ImageCampus.ToolBox.Services;
using UnityEngine;

public class FloatingTextInstancer : IService
{
    private GameObject _floatingTextGO;
    private GameObject _canvasGO;
    private FloatingText _floatingText;

    public bool IsPersistance => false;

    public FloatingTextInstancer(GameObject floatingTextGO, GameObject canvasGO)
    {
        _floatingTextGO = floatingTextGO;
        _canvasGO = canvasGO;
    }

    public GameObject InstantiateText(string text, Vector3 worldPosition, Color color, float scale = 2f, Transform parent = null)
    {
        Canvas canvas = _canvasGO.GetComponent<Canvas>();
        RectTransform canvasRect = _canvasGO.GetComponent<RectTransform>();

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        Camera uiCamera = null;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPosition);
        Transform actualParent = parent != null ? parent : _canvasGO.transform;
        GameObject messageGO = Object.Instantiate(_floatingTextGO, actualParent);
        RectTransform rect = messageGO.GetComponent<RectTransform>();

        rect.anchoredPosition = localPosition;
        rect.localScale = Vector3.one * scale;

        messageGO.GetComponent<FloatingText>()?.SetTextAndColor(text, color);

        return messageGO;
    }
}
