using UnityEngine;

namespace MareBaixa
{
    // Espinho do Baiacu: voa reto, machuca Casco e some ao bater no chão ou depois de um tempo.
    // O chão é testado por consulta (OverlapCircle), porque um corpo Kinematic não recebe
    // eventos de trigger contra o tilemap estático.
    //
    // Prefab: SpriteRenderer (sorting 15, arte espinho apontando para a direita) + Rigidbody2D
    // Kinematic + CircleCollider2D trigger (raio ~0.15) + Spike.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Spike : MonoBehaviour
    {
        public float lifetime = 1.2f;
        public float groundProbe = .08f;

        Rigidbody2D body;
        Vector2 velocity;
        float dieAt;
        int groundMask;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            groundMask = LayerMask.GetMask("Chao");
            dieAt = Time.time + lifetime;
        }

        public void Launch(Vector2 newVelocity)
        {
            velocity = newVelocity;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            dieAt = Time.time + lifetime;
        }

        void FixedUpdate()
        {
            Vector2 next = body.position + velocity * Time.fixedDeltaTime;
            body.MovePosition(next);
            if (Time.time >= dieAt || Physics2D.OverlapCircle(next, groundProbe, groundMask) != null)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            // Rolando (ou piscando depois de um golpe) o espinho passa direto.
            if (casco == null || casco.Dead || casco.Invulnerable) return;
            casco.Hurt(transform.position);
            Destroy(gameObject);
        }
    }
}
