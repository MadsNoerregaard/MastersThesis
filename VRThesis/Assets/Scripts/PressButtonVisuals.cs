using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PressButtonVisuals : MonoBehaviour
{
    public Transform buttonTop;
    public Vector3 localPressedOffset = new Vector3(0f, -0.03f, 0f);

    public float pressDownTime = 0.12f;
    public float holdTime = 0.08f;
    public float releaseTime = 0.12f;

    public UnityEvent onPressed;

    private Vector3 _startLocalPos;
    private bool _isPressing;

    private void Awake()
    {
        if (buttonTop != null)
            _startLocalPos = buttonTop.localPosition;
    }

    public void Press()
    {
        if (_isPressing) return;
        StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        _isPressing = true;

        Vector3 pressedPos = _startLocalPos + localPressedOffset;

        yield return MoveButton(_startLocalPos, pressedPos, pressDownTime);
        yield return new WaitForSeconds(holdTime);

        onPressed?.Invoke();

        yield return MoveButton(pressedPos, _startLocalPos, releaseTime);

        _isPressing = false;
    }

    private IEnumerator MoveButton(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            buttonTop.localPosition = Vector3.Lerp(from, to, normalized);

            yield return null;
        }

        buttonTop.localPosition = to;
    }
}
