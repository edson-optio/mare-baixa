using UnityEngine;

namespace MareBaixa
{
    // Caco de concha que voa, gira e some.
    public class Shard : MonoBehaviour
    {
        public Vector2 velocity;
        float age;

        void Update()
        {
            age += Time.deltaTime;
            velocity.y -= 25f * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            if (age > .9f) Destroy(gameObject);
        }
    }
}
