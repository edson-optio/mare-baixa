using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Algo que brilha no escuro da Fossa (Escuridao fura a escuridão em volta de cada uma).
    // Quem emite luz só adiciona este componente: peixe-pescador, coral bioluminescente, olhos do
    // Polvo. A de Casco é quente e acompanha casco.LightRadius sozinha; a dos bichos é fria.
    public class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();

        public float radius = 2.5f;
        public float flicker;          // 0 = firme; .2 = tremula 20% do raio
        public bool warm;              // ligado sozinho quando está em Casco
        public Vector2 offset;

        Casco casco;
        float seed;

        public Vector2 Position => (Vector2)transform.position + offset;

        // Raio deste quadro, já com a tremida.
        public float CurrentRadius
        {
            get
            {
                if (flicker <= 0f) return radius;
                float noise = Mathf.PerlinNoise(seed, Time.time * 3f) * 2f - 1f;
                return radius * (1f + flicker * noise);
            }
        }

        void Awake()
        {
            casco = GetComponentInParent<Casco>();
            if (casco != null)
            {
                warm = true;
                offset = new Vector2(0f, .35f);   // centro do corpo, como Casco.Center
            }
            seed = Random.value * 100f;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (casco != null) radius = casco.LightRadius;
        }
    }
}
