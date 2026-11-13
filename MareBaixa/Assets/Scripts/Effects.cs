using System.Collections;
using UnityEngine;

namespace MareBaixa
{
    // Sensação de impacto do GDD p. 6: pausa curta no golpe (hit-stop), brilho e cacos.
    public class Effects : MonoBehaviour
    {
        static Effects instance;
        float stopUntil;
        static Material trailMaterial;

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

        public static void Afterimage(SpriteRenderer source, Color color)
        {
            if (source == null || source.sprite == null) return;
            var go = new GameObject("Rastro da Espiral", typeof(SpriteRenderer), typeof(CombatTrail));
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            go.transform.localScale = source.transform.lossyScale;
            var art = go.GetComponent<SpriteRenderer>();
            art.sprite = source.sprite;
            art.sortingOrder = source.sortingOrder - 1;
            art.color = new Color(color.r, color.g, color.b, .5f);
        }

        public static void Slash(Vector2 center, float facing, float reach, Color color, bool thrust)
        {
            if (trailMaterial == null) trailMaterial = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("Rastro do golpe", typeof(LineRenderer), typeof(CombatTrail));
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = trailMaterial;
            line.sortingOrder = 24;
            line.startWidth = .09f;
            line.endWidth = .025f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, .25f);
            line.positionCount = thrust ? 2 : 12;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / (line.positionCount - 1);
                float angle = Mathf.Lerp(55f, -50f, t) * Mathf.Deg2Rad;
                Vector2 point = thrust ? new Vector2(Mathf.Lerp(.45f, reach, t) * facing, .05f)
                    : new Vector2(Mathf.Cos(angle) * reach * facing, Mathf.Sin(angle) * .6f);
                line.SetPosition(i, center + point);
            }
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
