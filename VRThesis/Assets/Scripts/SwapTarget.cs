using UnityEngine;

public class SwapTarget : MonoBehaviour
{
    [Tooltip("Reference point the player should align to when swapping.")]
    public Transform reference;

    [Tooltip("Optional visuals to hide after the swap.")]
    public GameObject visualsRoot;

    [Tooltip("Material the player's hands should use after swapping.")]
    public Material playerHandMaterial;
    
    [Tooltip("The hand spawn center of the target hands.")]
    public Transform handSpawnCenter;

    [Header("Assist Position and Rotation Offsets")]
    public Vector3 leftHandPositionOffset;
    public Vector3 rightHandPositionOffset;
    public Vector3 leftHandRotationOffset;
    public Vector3 rightHandRotationOffset;

    [Header("Autonomy Behaviour")]
    public AutonomyBehaviourType leftHandBehaviour = AutonomyBehaviourType.None;
    public AutonomyBehaviourType rightHandBehaviour = AutonomyBehaviourType.None;

    public void SetVisible(bool visible)
    {
        if (visualsRoot != null)
            visualsRoot.SetActive(visible);
    }
}