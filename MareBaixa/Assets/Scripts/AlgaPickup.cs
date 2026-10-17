using UnityEngine;

namespace MareBaixa
{
    // Alga solta no recife: Casco guarda até GameRules.MaxAlgae na bolsa e come com Q para
    // consertar a concha. Com a bolsa cheia ela fica onde está.
    //
    // Prefab: SpriteRenderer (sorting 10, alga) + Rigidbody2D Kinematic + CircleCollider2D trigger
    // (raio 0.35) + AlgaPickup.
    [RequireComponent(typeof(Rigidbody2D))]
    public class AlgaPickup : MonoBehaviour
    {
        public float swayAngle = 8f;
        public float swayPeriod = 2.4f;
        public float bobHeight = .06f;

        Vector3 home;
        float phase;
        bool taken;

        void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            home = transform.position;
            phase = Random.value * swayPeriod;
        }

        // Balança devagar como se a corrente a empurrasse.
        void Update()
        {
            float wave = Mathf.Sin((Time.time + phase) / swayPeriod * Mathf.PI * 2f);
            transform.position = home + new Vector3(0f, wave * bobHeight, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, wave * swayAngle);
        }

        // Stay e não Enter: se Casco comer uma alga parado em cima desta, ela é pega em seguida.
        void OnTriggerStay2D(Collider2D other)
        {
            if (taken) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead || !casco.AddAlga()) return;
            taken = true;
            Director.Instance?.Sound?.Play(Sfx.Equip);
            Effects.Spark(transform.position);
            Destroy(gameObject);
        }
    }
}
