using UnityEngine;

public class ReturnTriggerObject : MonoBehaviour
{
    private HandSwapManager _swapManager;
    private bool _used;

    public void Initialize(HandSwapManager manager)
    {
        _swapManager = manager;
        _used = false;
    }

    public void OnPoked()
    {
        if (_used) return;
        _used = true;

        if (_swapManager != null)
            _swapManager.NotifySpawnedObjectInteracted();
    }
}