using UnityEngine;

namespace MareBaixa
{
    // Válvula da Cidade Afundada: um beliscão gira o registro e a água baixa de uma vez por alguns
    // segundos (Mare.Drain). O barulho chama os inimigos por perto (Enemy.Alert). Depois ela
    // precisa de um tempo para poder girar de novo; pronta, ela brilha de leve (GDD p. 7).
    //
    // Prefab: SpriteRenderer (sorting 10, valvula-1, pivô nos pés) + BoxCollider2D trigger
    // (0.9 × 0.9, offset 0, 0.45) + Valvula. Sem Rigidbody2D: o golpe de Casco é uma consulta.
    // Preencher frames (valvula-1/2).
    [RequireComponent(typeof(SpriteRenderer))]
    public class Valvula : MonoBehaviour, IHittable
    {
        public Sprite[] frames;
        public float drainBlocks = 3f;
        public float drainHold = 5f;
        public float alertRadius = 12f;
        public float rechargeTime = 12f;
        public float spinTime = .6f;

        static readonly Color Spent = new Color(.55f, .55f, .6f, 1f);

        SpriteRenderer spriteRenderer;
        float readyAt;
        float turnedAt = -10f;
        float nextBubble;

        public bool Ready => Time.time >= readyAt;
        public int Turns { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[0];
        }

        public void Hit(int damage, Vector2 from)
        {
            if (!Ready)
            {
                Effects.Spark(from);
                Director.Instance?.Sound?.Play(Sfx.Error);
                return;
            }
            Turn();
        }

        // Também serve para o teste automático.
        public void Turn()
        {
            readyAt = Time.time + rechargeTime;
            turnedAt = Time.time;
            Turns++;
            Vector2 position = transform.position;
            Mare.Current?.Drain(drainBlocks, drainHold);
            Enemy.Alert(position, alertRadius);
            Director.Instance?.Sound?.Play(Sfx.Valve);
            Director.Instance?.CameraRig.Shake(.1f, .3f);
            Effects.Spark(position + new Vector2(0f, .5f));
        }

        void Update()
        {
            float spinning = Time.time - turnedAt;
            bool turning = spinning < spinTime;
            if (frames != null && frames.Length > 0)
            {
                // Girando, troca de quadro rápido; parada, fica no primeiro.
                int index = turning ? Mathf.FloorToInt(spinning * 16f) % frames.Length : 0;
                spriteRenderer.sprite = frames[index];
            }

            // Bolhas saindo do cano enquanto a água escoa.
            if (spinning < drainHold && Time.time >= nextBubble)
            {
                nextBubble = Time.time + .35f;
                Effects.Spark((Vector2)transform.position + new Vector2(Random.Range(-.4f, .4f), Random.Range(.6f, 1.4f)));
            }

            if (Ready)
            {
                float glow = .9f + Mathf.Sin(Time.time * 3f) * .1f;
                spriteRenderer.color = new Color(glow, glow, glow, 1f) + new Color(0f, .1f, .08f, 0f);
            }
            else
            {
                // Recarregando: apagada, e clareia conforme fica pronta.
                float charge = 1f - (readyAt - Time.time) / rechargeTime;
                spriteRenderer.color = Color.Lerp(Spent, Color.white, charge * .5f);
            }
        }
    }
}
