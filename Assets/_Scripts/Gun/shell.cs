using UnityEngine;

public class Shell : MonoBehaviour
{
    Transform shellEjectPoint;
    public float lifeTime = 20f;
    public float ejectionForceMultiplier = -5f; // Strength of the impulse

    private void Start()
    {
        // Add a Rigidbody component if it doesn't exist
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Apply the impulse force
        rb.AddForce(-transform.right * (ejectionForceMultiplier + Random.Range(-0.1f, -0.5f)), ForceMode.Impulse);

        // Destroy the object after its lifetime
        Destroy(gameObject, lifeTime);
    }

}