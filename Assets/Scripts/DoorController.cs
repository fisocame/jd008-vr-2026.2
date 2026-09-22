using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour, IActivatable
{
    public Transform doorHinge;
    public Vector3 closedEuler = Vector3.zero;
    public Vector3 openEuler = new Vector3(0f, 90f, 0f);
    public float speed = 3f;
    Coroutine co;

    void Reset() { doorHinge = transform; }
    public void Activate() => StartMove(openEuler);
    public void Deactivate() => StartMove(closedEuler);

    void StartMove(Vector3 targetEuler)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Move(targetEuler));
    }

    IEnumerator Move(Vector3 targetEuler)
    {
        var start = doorHinge.localRotation;
        var target = Quaternion.Euler(targetEuler);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            doorHinge.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }
        doorHinge.localRotation = target;
    }
}

public interface IActivatable { void Activate(); void Deactivate(); }