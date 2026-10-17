using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Texto de tutorial que aparece quando Casco passa perto (linhas @dica dos arquivos de sala).
    public class Hint : MonoBehaviour
    {
        public static readonly List<Hint> All = new List<Hint>();
        [TextArea] public string text;
        public float radius = 3f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static string Near(Vector2 position)
        {
            Hint best = null;
            float bestDistance = float.MaxValue;
            foreach (var hint in All)
            {
                float dx = Mathf.Abs(position.x - hint.transform.position.x);
                if (dx <= hint.radius && dx < bestDistance
                    && Mathf.Abs(position.y - hint.transform.position.y) <= hint.radius + 2f)
                {
                    best = hint;
                    bestDistance = dx;
                }
            }
            return best != null ? best.text : null;
        }
    }
}
