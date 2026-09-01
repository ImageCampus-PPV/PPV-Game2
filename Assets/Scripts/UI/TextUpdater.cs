using TMPro;
using UnityEngine;

public class TextUpdater : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textMesh;

    private void Awake()
    {
        if (_textMesh != null)
            return;
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void ChangeText(string newText)
    {
        if (_textMesh != null)
            _textMesh.text = newText;
    }

    public void ChangeColor(Color newColor)
    {
        if (_textMesh != null)
            _textMesh.color = newColor;
    }

    public Color GetColor()
    {
        return _textMesh.color;
    }

    public GameObject GetTextObject()
    {
        return _textMesh.gameObject;
    }
}
