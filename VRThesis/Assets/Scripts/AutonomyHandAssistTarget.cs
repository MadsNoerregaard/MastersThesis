using UnityEngine;
using UnityEngine.Events;

public class AutonomyHandAssistTarget : MonoBehaviour
{
    public Transform leftAssistAnchor;
    public Transform rightAssistAnchor;
    public Transform pointTarget;
    public float activationRadius = 0.12f;
    public PressButtonVisuals buttonVisuals;

    [HideInInspector] public Vector3 leftAssistPositionOffset;
    [HideInInspector] public Vector3 rightAssistPositionOffset;
    [HideInInspector] public Vector3 leftAssistRotationOffset;
    [HideInInspector] public Vector3 rightAssistRotationOffset;
    

    public UnityEvent onAssistPress;
    [HideInInspector] public bool isPressed = false;

    public bool requireCurrentSwappedTarget = true;

    public Transform GetAnchor(bool isLeft)
    {
        return isLeft ? leftAssistAnchor : rightAssistAnchor;
    }

    public Vector3 GetAssistPosition(bool isLeft)
    {
        Transform assistAnchor = GetAnchor(isLeft);
        if (assistAnchor == null)
            return Vector3.zero;

        Vector3 offset = isLeft ? leftAssistPositionOffset : rightAssistPositionOffset;
        return assistAnchor.TransformPoint(offset);
    }
    
    public Quaternion GetAssistRotation(bool isLeft)
    {
        Transform assistAnchor = GetAnchor(isLeft);
        if (assistAnchor == null)
            return Quaternion.identity;

        Vector3 offset = isLeft ? leftAssistRotationOffset : rightAssistRotationOffset;
        return assistAnchor.rotation * Quaternion.Euler(offset);
    }

    public Quaternion ApplyOffsetRotation(bool isLeft, Quaternion inputRotation)
    {
        Vector3 offset = isLeft ? leftAssistRotationOffset : rightAssistRotationOffset;
        return inputRotation * Quaternion.Euler(offset);
    }

    public void TriggerAutoPress()
    {
        if (isPressed) return;
        isPressed = true;
        onAssistPress?.Invoke();
    }

    public void ResetAutoPress()
    {
        isPressed = false;
    }
}
