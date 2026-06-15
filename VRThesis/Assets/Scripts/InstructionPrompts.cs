using UnityEngine;
using TMPro;

public class InstructionPrompts : MonoBehaviour
{
    public Transform playerHead;
    public TextMeshProUGUI text;

    public float distanceFromPlayer = 1.5f;
    public float verticalOffset = 0f;
    public bool followPlayer = true;

    private bool _visible;

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        if (_visible && followPlayer)
            PlaceInFrontOfPlayer();
    }

    public void Show(string message)
    {
        if (text)
            text.text = message;

        _visible = true;
        gameObject.SetActive(true);
        PlaceInFrontOfPlayer();
    }

    public void Hide()
    {
        _visible = false;
        gameObject.SetActive(false);
    }

    private void PlaceInFrontOfPlayer()
    {
        if (!playerHead) return;

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position =
            playerHead.position +
            forward * distanceFromPlayer +
            Vector3.up * verticalOffset;

        transform.rotation = Quaternion.LookRotation(forward);
    }
}