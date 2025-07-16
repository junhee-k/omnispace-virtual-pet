using UnityEngine;
#if UNITY_INCLUDE_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
#endif

namespace PolySpatial.Samples
{
    public class PinchSpawn : MonoBehaviour
    {
        [SerializeField]
        GameObject m_RightSpawnPrefab;

        [SerializeField]
        GameObject m_LeftSpawnPrefab;

        [SerializeField]
        [Tooltip("If set, a pinch will only trigger a spawn if it occurs inside this Collider.")]
        Collider m_activationCollider;

        [SerializeField]
        Transform m_PolySpatialCameraTransform;

#if UNITY_INCLUDE_XR_HANDS
        XRHandSubsystem m_HandSubsystem;
        XRHandJoint m_RightIndexTipJoint;
        XRHandJoint m_RightThumbTipJoint;
        XRHandJoint m_LeftIndexTipJoint;
        XRHandJoint m_LeftThumbTipJoint;
        bool m_ActiveRightPinch;
        bool m_ActiveLeftPinch;
        float m_ScaledThreshold;

        const float k_PinchThreshold = 0.02f;

        void Start()
        {
            Debug.Log("PinchSpawn Start called");
            GetHandSubsystem();
            Debug.Log("PinchSpawn Start called 2");
            m_ScaledThreshold = k_PinchThreshold / m_PolySpatialCameraTransform.localScale.x;
            Debug.Log("PinchSpawn Start called 3");
        }

        void Update()
        {
            Debug.Log("PinchSpawn Update called");

            if (!CheckHandSubsystem())
                return;


            var updateSuccessFlags = m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);

            Debug.Log("Update Success Flags: " + updateSuccessFlags);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != 0)
            {
                // assign joint values
                m_RightIndexTipJoint = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.IndexTip);
                m_RightThumbTipJoint = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);

                DetectPinch(m_RightIndexTipJoint, m_RightThumbTipJoint, ref m_ActiveRightPinch, true);
            }

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != 0)
            {
                // assign joint values
                m_LeftIndexTipJoint = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.IndexTip);
                m_LeftThumbTipJoint = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip);

                DetectPinch(m_LeftIndexTipJoint, m_LeftThumbTipJoint, ref m_ActiveLeftPinch, false);
            }
        }

        void GetHandSubsystem()
        {
            var xrGeneralSettings = XRGeneralSettings.Instance;
            if (xrGeneralSettings == null)
            {
                Debug.LogError("XR general settings not set");
                return; // Added return here
            }
            Debug.Log("XR general settings found."); // Added log

            var manager = xrGeneralSettings.Manager;
            if (manager != null)
            {
                Debug.Log("XRManagerSettings found."); // Added log
                var loader = manager.activeLoader;
                if (loader != null)
                {
                    Debug.Log("Active loader found."); // Added log
                    m_HandSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
                    if (!CheckHandSubsystem())
                    {
                        Debug.LogError("Hand Subsystem check failed immediately after loading."); // Added log
                        return;
                    }

                    m_HandSubsystem.Start();
                    Debug.Log("Hand Subsystem started successfully.");
                }
                else
                {
                    Debug.LogError("No active loader found."); // Added log
                }
            }
            else
            {
                Debug.LogError("XRManagerSettings not found."); // Added log
            }
        }

        bool CheckHandSubsystem()
        {
            if (m_HandSubsystem == null)
            {
#if !UNITY_EDITOR
                Debug.LogError("Could not find Hand Subsystem");
#endif
                enabled = false;
                return false;
            }

            return true;
        }

        void DetectPinch(XRHandJoint index, XRHandJoint thumb, ref bool activeFlag, bool right)
        {
            var spawnObject = right ? m_RightSpawnPrefab : m_LeftSpawnPrefab;

            if (spawnObject == null)
                return;

            if (index.trackingState != XRHandJointTrackingState.None &&
                thumb.trackingState != XRHandJointTrackingState.None)
            {
                Vector3 indexPOS = Vector3.zero;
                Vector3 thumbPOS = Vector3.zero;

                Debug.Log("thumb: " + thumb.trackingState + ", index: " + index.trackingState);

                if (index.TryGetPose(out Pose indexPose))
                {
                    // adjust transform relative to the PolySpatial Camera transform
                    indexPOS = m_PolySpatialCameraTransform.InverseTransformPoint(indexPose.position);
                }

                if (thumb.TryGetPose(out Pose thumbPose))
                {
                    // adjust transform relative to the PolySpatial Camera adjustments
                    thumbPOS = m_PolySpatialCameraTransform.InverseTransformPoint(thumbPose.position);
                }

                var pinchDistance = Vector3.Distance(indexPOS, thumbPOS);
                var pinchPosition = (indexPose.position + thumbPose.position) / 2.0f;

                if (pinchDistance <= m_ScaledThreshold)
                {
                    if (!activeFlag)
                    {
                        if(m_activationCollider != null && m_activationCollider.bounds.Contains(pinchPosition))
                        {
                            Instantiate(spawnObject, pinchPosition, Quaternion.identity);
                            activeFlag = true;
                        }
                    }
                }
                else
                {
                    activeFlag = false;
                }
            }
        }
#endif
    }
}
