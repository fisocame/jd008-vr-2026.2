using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ShortRangePull : MonoBehaviour
{
    public XRDirectInteractor directInteractor;
    public Transform pullTarget;
    public InputActionReference pullAction;
    public float maxDistance = 1.2f;
    public float searchRadius = 0.25f;
    public float pullSpeed = 8f;
    public LayerMask obstructionMask = ~0;
    public LayerMask candidateMask = ~0;
    public bool requireGrabbable = false;

    bool _pullHeld;
    bool _pulling;
    Collider _currentCol;
    XRGrabInteractable _currentGrab;
    Rigidbody _currentRb;

    bool _hadGravity;
    bool _hadKinematic;
    RigidbodyConstraints _hadConstraints;
    CollisionDetectionMode _hadCCD;

    void Reset()
    {
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        if (!pullTarget && directInteractor) pullTarget = directInteractor.attachTransform;
    }

    void OnEnable()
    {
        if (pullAction != null)
        {
            pullAction.action.performed += OnPullPerformed;
            pullAction.action.canceled += OnPullCanceled;
        }
    }

    void OnDisable()
    {
        if (pullAction != null)
        {
            pullAction.action.performed -= OnPullPerformed;
            pullAction.action.canceled -= OnPullCanceled;
        }
        StopPullCoroutineIfAny();
        ForceRestorePhysicsIfAny();
        UnhookInteractable();
        ClearCurrent();
    }

    void OnPullPerformed(InputAction.CallbackContext _)
    {
        _pullHeld = true;
        if (_pulling || !directInteractor || directInteractor.hasSelection) return;

        var col = FindNearestColliderCandidate();
        if (col) StartCoroutine(PullAndMaybeGrab(col));
    }

    void OnPullCanceled(InputAction.CallbackContext _)
    {
        _pullHeld = false;

        if (directInteractor && directInteractor.hasSelection)
            directInteractor.EndManualInteraction();

        StopPullCoroutineIfAny();
        RestorePhysics();
        UnhookInteractable();
        ClearCurrent();
    }

    Collider FindNearestColliderCandidate()
    {
        Vector3 origin = transform.position;
        Vector3 fwd = transform.forward;

        var hits = Physics.SphereCastAll(origin, searchRadius, fwd, maxDistance, candidateMask, QueryTriggerInteraction.Ignore);

        var ordered = hits
            .Select(h => h.collider)
            .Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy)
            .Distinct()
            .OrderBy(c => Vector3.Distance(origin, c.bounds.center));

        foreach (var col in ordered)
        {
            if (requireGrabbable && !col.GetComponentInParent<XRGrabInteractable>()) continue;

            Vector3 targetPoint = col.bounds.center;
            Vector3 dir = targetPoint - origin;
            float dist = dir.magnitude;

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstructionMask, QueryTriggerInteraction.Ignore))
                if (hit.transform.root != col.transform.root) continue;

            return col;
        }

        return null;
    }

    IEnumerator PullAndMaybeGrab(Collider col)
    {
        _pulling = true;
        _currentCol = col;
        _currentGrab = col.GetComponentInParent<XRGrabInteractable>();
        _currentRb = col.attachedRigidbody;
        HookInteractable(_currentGrab);

        if (_currentRb)
        {
            _hadGravity = _currentRb.useGravity;
            _hadKinematic = _currentRb.isKinematic;
            _hadConstraints = _currentRb.constraints;
            _hadCCD = _currentRb.collisionDetectionMode;

            _currentRb.useGravity = false;
            _currentRb.isKinematic = false;
            _currentRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            SafeZeroVel(_currentRb);
        }

        Transform t = col.transform;

        while (_pullHeld && _currentCol && pullTarget && Vector3.Distance(t.position, pullTarget.position) > 0.06f)
        {
            if (_currentRb)
            {
                Vector3 next = Vector3.Lerp(t.position, pullTarget.position, Time.deltaTime * pullSpeed);
                _currentRb.MovePosition(next);
                SafeZeroVel(_currentRb);
            }
            else
            {
                t.position = Vector3.Lerp(t.position, pullTarget.position, Time.deltaTime * pullSpeed);
            }

            if (directInteractor && directInteractor.hasSelection) break;
            yield return null;
        }

        if (_pullHeld && _currentGrab && directInteractor && !directInteractor.hasSelection)
            directInteractor.StartManualInteraction(_currentGrab);

        _pulling = false;
    }

    void HookInteractable(XRGrabInteractable grab)
    {
        if (!grab) return;
        grab.selectEntered.AddListener(OnItemSelected);
        grab.selectExited.AddListener(OnItemReleased);
    }

    void UnhookInteractable()
    {
        if (!_currentGrab) return;
        _currentGrab.selectEntered.RemoveListener(OnItemSelected);
        _currentGrab.selectExited.RemoveListener(OnItemReleased);
    }

    void OnItemSelected(SelectEnterEventArgs args)
    {
        StopPullCoroutineIfAny();
    }

    void OnItemReleased(SelectExitEventArgs args)
    {
        RestorePhysics();
        UnhookInteractable();
        ClearCurrent();
    }

    void StopPullCoroutineIfAny()
    {
        if (_pulling)
        {
            StopAllCoroutines();
            _pulling = false;
        }
    }

    void RestorePhysics()
    {
        if (_currentRb)
        {
            _currentRb.useGravity = _hadGravity;
            _currentRb.isKinematic = _hadKinematic;
            _currentRb.collisionDetectionMode = _hadCCD;
            _currentRb.constraints = _hadConstraints;
            SafeZeroVel(_currentRb);
        }
    }

    void ForceRestorePhysicsIfAny()
    {
        if (_currentRb)
        {
            _currentRb.useGravity = true;
            _currentRb.isKinematic = false;
        }
    }

    void SafeZeroVel(Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    void ClearCurrent()
    {
        _currentCol = null;
        _currentGrab = null;
        _currentRb = null;
    }
}