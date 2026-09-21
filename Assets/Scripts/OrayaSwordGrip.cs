using UnityEngine;

[ExecuteAlways]
public class OrayaSwordGrip : MonoBehaviour
{
    Transform _lowerR;
    Transform _handR;
    Transform _lowerL;
    Transform _handL;

    static readonly Vector3 LowerREuler = new Vector3(0f, 0f, 92f);
    static readonly Vector3 HandREuler = new Vector3(33.87f, 343.94f, 168.61f);
    static readonly Vector3 LowerLEuler = new Vector3(0f, 0f, 36.7f);
    static readonly Vector3 HandLEuler = new Vector3(287.1f, 1.7f, 347.3f);

    static readonly (string path, Vector3 euler)[] RightPoses =
    {
        ("index_metacarpal_r/index_01_r", new Vector3(351.45f, 7.70f, 288f)),
        ("index_metacarpal_r/index_01_r/index_02_r", new Vector3(0.01f, 359.74f, 278f)),
        ("index_metacarpal_r/index_01_r/index_02_r/index_03_r", new Vector3(0f, 359.94f, 300f)),
        ("middle_metacarpal_r/middle_01_r", new Vector3(358.39f, 5.41f, 284f)),
        ("middle_metacarpal_r/middle_01_r/middle_02_r", new Vector3(359.93f, 359.52f, 272f)),
        ("middle_metacarpal_r/middle_01_r/middle_02_r/middle_03_r", new Vector3(0f, 0.22f, 298f)),
        ("ring_metacarpal_r/ring_01_r", new Vector3(359.13f, 359.47f, 286f)),
        ("ring_metacarpal_r/ring_01_r/ring_02_r", new Vector3(359.84f, 359.58f, 270f)),
        ("ring_metacarpal_r/ring_01_r/ring_02_r/ring_03_r", new Vector3(0f, 0.37f, 296f)),
        ("pinky_metacarpal_r/pinky_01_r", new Vector3(0.03f, 358.86f, 290f)),
        ("pinky_metacarpal_r/pinky_01_r/pinky_02_r", new Vector3(359.97f, 0.21f, 268f)),
        ("pinky_metacarpal_r/pinky_01_r/pinky_02_r/pinky_03_r", new Vector3(0f, 0.08f, 294f)),
        ("thumb_01_r", new Vector3(48f, 312f, 300f)),
        ("thumb_01_r/thumb_02_r", new Vector3(1.17f, 6.28f, 290f)),
        ("thumb_01_r/thumb_02_r/thumb_03_r", new Vector3(0f, 359.80f, 310f)),
    };

    static readonly (string path, Vector3 euler)[] LeftPoses =
    {
        ("index_metacarpal_l/index_01_l", new Vector3(351.45f, 7.70f, 292f)),
        ("index_metacarpal_l/index_01_l/index_02_l", new Vector3(0.01f, 359.74f, 282f)),
        ("index_metacarpal_l/index_01_l/index_02_l/index_03_l", new Vector3(0f, 359.94f, 305f)),
        ("middle_metacarpal_l/middle_01_l", new Vector3(358.39f, 5.41f, 290f)),
        ("middle_metacarpal_l/middle_01_l/middle_02_l", new Vector3(359.93f, 359.52f, 276f)),
        ("middle_metacarpal_l/middle_01_l/middle_02_l/middle_03_l", new Vector3(0f, 0.22f, 302f)),
        ("ring_metacarpal_l/ring_01_l", new Vector3(359.13f, 359.47f, 292f)),
        ("ring_metacarpal_l/ring_01_l/ring_02_l", new Vector3(359.84f, 359.58f, 274f)),
        ("ring_metacarpal_l/ring_01_l/ring_02_l/ring_03_l", new Vector3(0f, 0.37f, 300f)),
        ("pinky_metacarpal_l/pinky_01_l", new Vector3(0.03f, 358.86f, 294f)),
        ("pinky_metacarpal_l/pinky_01_l/pinky_02_l", new Vector3(359.97f, 0.21f, 272f)),
        ("pinky_metacarpal_l/pinky_01_l/pinky_02_l/pinky_03_l", new Vector3(0f, 0.08f, 298f)),
        ("thumb_01_l", new Vector3(52f, 302f, 308f)),
        ("thumb_01_l/thumb_02_l", new Vector3(1.17f, 6.28f, 296f)),
        ("thumb_01_l/thumb_02_l/thumb_03_l", new Vector3(0f, 359.80f, 318f)),
    };

    void OnEnable() => Cache();

    void Cache()
    {
        _lowerR = transform.Find("root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_r/upperarm_r/lowerarm_r");
        _handR = _lowerR != null ? _lowerR.Find("hand_r") : null;
        _lowerL = transform.Find("root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_l/upperarm_l/lowerarm_l");
        _handL = _lowerL != null ? _lowerL.Find("hand_l") : null;
    }

    void LateUpdate() => Apply();

    void Apply()
    {
        if (_handR == null || _handL == null) Cache();
        ApplyArm(_lowerR, _handR, LowerREuler, HandREuler, RightPoses);
        ApplyArm(_lowerL, _handL, LowerLEuler, HandLEuler, LeftPoses);
    }

    static void ApplyArm(Transform lower, Transform hand, Vector3 lowerEuler, Vector3 handEuler, (string path, Vector3 euler)[] poses)
    {
        if (lower == null || hand == null) return;
        lower.localRotation = Quaternion.Euler(lowerEuler);
        hand.localRotation = Quaternion.Euler(handEuler);
        for (int i = 0; i < poses.Length; i++)
        {
            var t = hand.Find(poses[i].path);
            if (t != null)
                t.localRotation = Quaternion.Euler(poses[i].euler);
        }
    }
}
