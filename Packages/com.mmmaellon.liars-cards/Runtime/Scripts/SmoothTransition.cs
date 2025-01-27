
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.LightSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmoothTransition : LightSyncState
    {
        [UdonSynced]
        public float transitionOffset = 0.1f;
        public float transitionTime = 0.25f;
        public Vector3 transitionStartVel = new Vector3(0, 25, 0);
        public Vector3 transitionEndVel = new Vector3(0, 25, 10);
        [UdonSynced, FieldChangeCallback(nameof(activePoint))]
        public int _activePoint = -1001;
        public int activePoint
        {
            get => _activePoint;
            set
            {
                _activePoint = value;
                if (!IsActiveState())
                {
                    SetParent();
                }
                if (sync.IsOwner())
                {
                    RequestSerialization();
                }
            }
        }
        Transform startingParent;
        public Transform[] transitionPoints = { };
        Vector3 startPos;
        Quaternion startRot;
        Vector3 targetPos;
        Quaternion targetRot;

        public void Start()
        {
            startingParent = transform.parent;
        }
        public override void OnEnterState()
        {
            startPos = transform.position;
            startRot = transform.rotation;
        }

        public override void OnExitState()
        {
            SetParent();
        }

        public void TransitionTo(int pointId, Vector3 localPos, Quaternion localRot)
        {
            sync.pos = localPos;
            sync.rot = localRot;
            _activePoint = pointId;
            EnterState();
            sync.StartLoop();
        }

        public override bool OnLerp(float elapsedTime, float autoSmoothedLerp)
        {
            if (elapsedTime < transitionOffset)
            {

                return true;
            }
            CalcTargetTransform();
            var lerp = (elapsedTime - transitionOffset) / transitionTime;

            transform.SetPositionAndRotation(sync.HermiteInterpolatePosition(startPos, transitionStartVel, targetPos, targetRot * -transitionEndVel, lerp, transitionTime), Quaternion.Slerp(startRot, targetRot, lerp));

            var shouldQuit = elapsedTime >= transitionTime + transitionOffset;
            if (shouldQuit)
            {
                SetParent();
            }
            return !shouldQuit;
        }

        public void SetParent()
        {
            if (_activePoint < 0 || _activePoint >= transitionPoints.Length)
            {
                transform.SetParent(startingParent, true);
            }
            else
            {
                transform.SetParent(transitionPoints[_activePoint], true);
            }
        }

        public void CalcTargetTransform()
        {
            var parent = startingParent;
            if (_activePoint >= 0 && _activePoint < transitionPoints.Length)
            {
                parent = transitionPoints[_activePoint];
            }
            if (parent)
            {
                targetPos = parent.position + parent.rotation * sync.pos;
                targetRot = parent.rotation * sync.rot;
            }
            else
            {
                targetPos = sync.pos;
                targetRot = sync.rot;
            }
        }
    }
}
