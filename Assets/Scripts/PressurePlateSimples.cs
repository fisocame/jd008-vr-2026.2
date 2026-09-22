using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlateSimple : MonoBehaviour
{
    public MonoBehaviour target;
    public float requiredMass = 1f;

    float totalMass;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null) totalMass += rb.mass;
        Evaluate();
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null) totalMass -= rb.mass;
        Evaluate();
    }

    void Evaluate()
    {
        bool on = totalMass >= requiredMass;
        if (target is IActivatable act)
        {
            if (on) act.Activate();
            else    act.Deactivate();
        }
    }
}