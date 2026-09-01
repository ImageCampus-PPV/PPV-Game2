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
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        worldPosition.z = 0f;
        GameObject messageGO = Object.Instantiate(_floatingTextGO, _canvasGO.transform);

        RectTransform rect = messageGO.GetComponent<RectTransform>();

        rect.anchoredPosition = screenPos;

        messageGO.transform.localScale = Vector2.one * scale;
        Debug.Log(messageGO.transform.parent.name);
        Debug.Log(rect.position);
        Debug.Log(rect.anchoredPosition);
        messageGO.GetComponent<FloatingText>()?.SetTextAndColor(text, color);
        return messageGO;
    }
}
