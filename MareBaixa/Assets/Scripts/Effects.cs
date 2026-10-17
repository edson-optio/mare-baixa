using System.Collections;
using UnityEngine;

namespace MareBaixa
{
    // Sensação de impacto do GDD p. 6: pausa curta no golpe (hit-stop), brilho e cacos.
    public class Effects : MonoBehaviour
    {
        static Effects instance;
        float stopUntil;

        static Effects Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("Efeitos").AddComponent<Effects>();
                return instance;
            }
        }

        public static void HitStop(float seconds)
        {
            var effects = Instance;
            bool running = Time.unscaledTime < effects.stopUntil;
            effects.stopUntil = Mathf.Max(effects.stopUntil, Time.unscaledTime + seconds);
            if (!running) effects.StartCoroutine(effects.Freeze());
        }

        IEnumerator Freeze()
        {
            Time.timeScale = 0f;
            while (Time.unscaledTime < stopUntil) yield return null;
            // Se o jogo foi pausado durante a pausa curta, quem pausou decide o timeScale.
            if (Director.Instance == null || !Director.Instance.Paused) Time.timeScale = 1f;
        }

        public static void Spark(Vector2 position)
        {
            var kit = Kit.Current;
            if (kit == null) return;
            var go = new GameObject("Brilho", typeof(SpriteRenderer));
            go.transform.position = position;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 45;
            var animator = go.AddComponent<SpriteAnimator>();
            animator.loop = false;
            animator.destroyWhenDone = true;
            animator.Play(kit.sparkFrames, 18f);
        }

        // A concha se desfaz em cacos que voam e somem.
        public static void Shards(Vector2 position, ShellType type)
        {
            var kit = Kit.Current;
            if (kit == null || type == ShellType.Nenhuma) return;
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Caco", typeof(SpriteRenderer));
                go.transform.position = position + Random.insideUnitCircle * .3f;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = kit.shardSprites[(int)type];
                renderer.sortingOrder = 44;
                go.AddComponent<Shard>().velocity = new Vector2(Random.Range(-5f, 5f), Random.Range(4f, 9f));
            }
        }
    }
}
