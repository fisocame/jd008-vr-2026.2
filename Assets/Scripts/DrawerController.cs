using System.Collections;
using UnityEngine;

public class DrawerController : MonoBehaviour, IActivatable
{
    public Transform drawer;
    public Vector3 closedLocalPos = Vector3.zero;
    public Vector3 openLocalPos = new Vector3(0f, 0f, 0.28f);
    public float speed = 3f;
    Coroutine co;

    void Reset() { drawer = transform; closedLocalPos = drawer.localPosition; }
    public void Activate() => StartMove(openLocalPos);
    public void Deactivate() => StartMove(closedLocalPos);

    void StartMove(Vector3 targetPos)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Move(targetPos));
    }

    IEnumerator Move(Vector3 targetPos)
    {
        var start = drawer.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            drawer.localPosition = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }
        drawer.localPosition = targetPos;
    }
}

// att 