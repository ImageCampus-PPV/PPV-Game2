using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField][Range(.1f, .2f)] private float _scrollSpeed = .1f;
    [SerializeField] private float _maxZoomDifference = 5;
    private float _zoom;

    [SerializeField] private float _moveSpeed = 15f;
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

    private void Start()
    {
        if (_camera == null)
            TryGetComponent(out _camera);

        _zoom = _camera.orthographicSize;
        _centerPosition = transform.position;

        _minPossiblePosition = new Vector3(
            _centerPosition.x - _maxMovement.x,
            _centerPosition.y - _maxMovement.y,
            _centerPosition.z
        );

        _maxPossiblePosition = new Vector3(
            _centerPosition.x + _maxMovement.x,
            _centerPosition.y + _maxMovement.y,
            _centerPosition.z
        );
    }

    private void Update()
    {
        ManageMovementByScreenBorder();
        ManageZoom();
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
            movement.y = _moveSpeed * Time.deltaTime;
        }
        else if (mousePosition.y <= Screen.height * 0.02f)
        {
            movement.y = -_moveSpeed * Time.deltaTime;
        }

        if (mousePosition.x >= Screen.width * 0.98f)
        {
            movement.x = _moveSpeed / 2 * Time.deltaTime;
            movement.z = -_moveSpeed / 2 * Time.deltaTime;
        }
        else if (mousePosition.x <= Screen.width * 0.02f)
        {
            movement.x = -_moveSpeed / 2 * Time.deltaTime;
            movement.z = _moveSpeed / 2 * Time.deltaTime;
        }

        Vector3 newPosition = transform.position + movement;

        newPosition.x = Mathf.Clamp(newPosition.x, _minPossiblePosition.x, _maxPossiblePosition.x);
        newPosition.y = Mathf.Clamp(newPosition.y, _minPossiblePosition.y, _maxPossiblePosition.y);
        newPosition.z = Mathf.Clamp(newPosition.z, _minPossiblePosition.x, _maxPossiblePosition.x);

        transform.position = newPosition;
    }

    [ContextMenu("Center camera")]
    private void CenterCamera()
    {
        transform.position = _centerPosition;
    }
}