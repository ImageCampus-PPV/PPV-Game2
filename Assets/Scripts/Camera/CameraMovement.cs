using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField][Range(.1f, .2f)] private float _scrollSpeed = .1f;
    [SerializeField] private float _maxZoomDifference = 5;
    private float _zoom;
    private float _defaultZoom;

    [SerializeField] private float _moveSpeedBorder = 15f;
    [SerializeField] private float _moveSpeedButton = 3f;
    [SerializeField][Range(0.8f, 1f)] private float _marginToDetect = .95f;
    [SerializeField] private Vector3 _maxMovement;

    private Camera _camera;

    private Vector3 _centerPosition;
    private Vector3 _minPossiblePosition;
    private Vector3 _maxPossiblePosition;

    private const float ZOOM_SPEED = 10.0f;
    private const float MIN_ZOOM = 3.0f;
    private const float MAX_ZOOM = 20.0f;
    private const float SMOOTH_TIME = 0.15f;
    private float _velocity = 0;

    private Vector3 _lastMousePosition;

    private void Start()
    {
        if (_camera == null)
            TryGetComponent(out _camera);

        _zoom = _camera.orthographicSize;
        _defaultZoom = _camera.orthographicSize;
        _centerPosition = transform.position;

        _minPossiblePosition = new Vector3(
            _centerPosition.x - _maxMovement.x,
            _centerPosition.y - _maxMovement.y,
            _centerPosition.z - _maxMovement.x
        );

        _maxPossiblePosition = new Vector3(
            _centerPosition.x + _maxMovement.x,
            _centerPosition.y + _maxMovement.y,
            _centerPosition.z + _maxMovement.x
        );
    }

    private void Update()
    {
        if (Input.GetMouseButton(2))
        {
            Debug.Log("WHEEL PRESSED");
            ManageMovementByMouseButton();
        }
        else
            ManageMovementByScreenBorder();

        ManageZoom();

        Mathf.Clamp(transform.position.x, _minPossiblePosition.x, _maxPossiblePosition.x);
        Mathf.Clamp(transform.position.y, _minPossiblePosition.y, _maxPossiblePosition.y);
        Mathf.Clamp(transform.position.z, _minPossiblePosition.x, _maxPossiblePosition.x);
    }

    private void ManageMovementByMouseButton()
    {
        if (Input.GetMouseButtonDown(2))
            _lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(2))
        {
            Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
            Vector3 movement = new Vector3(-mouseDelta.x / 2, -mouseDelta.y, mouseDelta.x / 2) * _camera.orthographicSize / _defaultZoom;

            transform.Translate(movement * _moveSpeedButton * Time.deltaTime, Space.Self);

            ClampPosition();
            _lastMousePosition = Input.mousePosition;
        }
    }

    private void ManageZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            _zoom -= scroll * ZOOM_SPEED;
            _zoom = Mathf.Clamp(_zoom, MIN_ZOOM, MAX_ZOOM);
        }

        _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _zoom, ref _velocity, SMOOTH_TIME);
    }

    private void ManageMovementByScreenBorder()
    {
        Vector3 movement = Vector3.zero;
        Vector3 mousePosition = Input.mousePosition;

        if (mousePosition.y >= Screen.height * 0.98f)
        {
            movement.y = _moveSpeedBorder * Time.deltaTime;
        }
        else if (mousePosition.y <= Screen.height * 0.02f)
        {
            movement.y = -_moveSpeedBorder * Time.deltaTime;
        }

        if (mousePosition.x >= Screen.width * 0.98f)
        {
            movement.x = _moveSpeedBorder / 2 * Time.deltaTime;
            movement.z = -_moveSpeedBorder / 2 * Time.deltaTime;
        }
        else if (mousePosition.x <= Screen.width * 0.02f)
        {
            movement.x = -_moveSpeedBorder / 2 * Time.deltaTime;
            movement.z = _moveSpeedBorder / 2 * Time.deltaTime;
        }

        transform.position += movement;
        ClampPosition();
    }

    [ContextMenu("Center camera")]
    private void CenterCamera()
    {
        transform.position = _centerPosition;
    }

    private void ClampPosition()
    {
        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, _minPossiblePosition.x, _maxPossiblePosition.x);
        position.y = Mathf.Clamp(position.y, _minPossiblePosition.y, _maxPossiblePosition.y);
        position.z = Mathf.Clamp(position.z, _minPossiblePosition.z, _maxPossiblePosition.z);

        transform.position = position;
    }

}