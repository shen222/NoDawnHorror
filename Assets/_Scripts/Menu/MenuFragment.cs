using UnityEngine;

public sealed class MenuFragment : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 spin;
    private float age;
    private void Update()
    {
        age += Time.deltaTime;
        velocity += Vector3.down * (5f * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(spin * Time.deltaTime, Space.Self);
        if (age >= 1.4f) Destroy(gameObject);
    }
}

