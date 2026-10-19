using System.Collections;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextUpdater _text;

    [SerializeField] private float _initialVelocityY;
    [SerializeField] private float _rangeVelocityX;
    [SerializeField] private float _lifeTime;

    private Vector3 _velocity;
    private GameObject _textObject;

    private void Awake()
    {
        if (_text != null)
            return;

        _text = GetComponent<TextUpdater>();
        if (_text == null)
            Debug.LogError("No text updater found");
    }

    private void Start()
    {
        _textObject = _text.GetTextObject();
        _velocity = new Vector3(Random.Range(-_rangeVelocityX, _rangeVelocityX), _initialVelocityY, 0f);

        Destroy(gameObject, _lifeTime);
    }

    private void Update()
    {
        _textObject.transform.position += _velocity * Time.deltaTime;
    }

    public void SetText(string text)
    {
        _text?.ChangeText(text);
    }

    public void SetTextAndColor(string text, Color color)
    {
        _text?.ChangeText(text);
        _text?.ChangeColor(color);
    }
}