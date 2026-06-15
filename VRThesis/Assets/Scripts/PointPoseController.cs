using UnityEngine;

public class PointPoseController : MonoBehaviour
{
    [System.Serializable]
    public class BonePose
    {
        public Transform bone;
        public Vector3 pointingLocalEuler;
    }

    public BonePose[] bones;
    public float blendSpeed = 10f;

    private float _weight;

    public void SetPointingWeight(float target)
    {
        _weight = Mathf.MoveTowards(_weight, target, Time.deltaTime * blendSpeed);
    }

    void LateUpdate()
    {
        foreach (var b in bones)
        {
            if (b.bone == null) continue;

            Quaternion targetRot = Quaternion.Euler(b.pointingLocalEuler);
            b.bone.localRotation = Quaternion.Slerp(
                b.bone.localRotation,
                targetRot,
                _weight
            );
        }
    }
}