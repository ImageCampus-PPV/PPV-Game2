using ImageCampus.ToolBox.Services;
using UnityEngine;
using UnityEngine.InputSystem;

public static class HoverChecker
{
    public static bool CheckHover<T>(out T anyObj, bool checkIfHoverAvailable = true)
    {
        anyObj = default;

        if (checkIfHoverAvailable)
        {
            if (!CanCheckHover())
                return false;
        }

        Vector3 screenPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        RaycastHit2D[] hits = Physics2D.RaycastAll(screenPos, Vector2.zero);

        foreach (var hit in hits)
        {
            if (hit.collider == null) 
                continue;

            if (hit.collider.gameObject.TryGetComponent<T>(out anyObj))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckHover(Collider2D collider, bool checkIfHoverAvailable = true)
    {
        if (checkIfHoverAvailable)
        {
            if (!CanCheckHover())
                return false;
        }

        var screenPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        var hits = Physics2D.RaycastAll(screenPos, Vector2.zero);

        foreach (var hit in hits)
        {
            if (hit.collider == collider)
                return true;
        }

        return false;
    }

    public static bool CheckHoverNoRay(Bounds bounds, bool checkIfHoverAvailable = true)
    {
        if (checkIfHoverAvailable)
        {
            if (!CanCheckHover())
                return false;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;
        worldMousePos.z = bounds.center.z;

        return bounds.Contains(worldMousePos);
    }

    public static bool CanCheckHover()
    {
        //In case any condition is needed

        return true;
    }
}