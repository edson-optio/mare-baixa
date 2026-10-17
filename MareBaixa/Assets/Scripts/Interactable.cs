using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Algo que Casco usa com E: conchas no chão, moradores da vila, a descida para o Recife.
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public float radius = 1.1f;

        public abstract string Prompt { get; }
        public abstract void Interact(Casco casco);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public static Interactable Closest(Vector2 position)
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            foreach (var item in All)
            {
                float distance = Vector2.Distance(position, item.transform.position);
                if (distance <= item.radius && distance < bestDistance)
                {
                    best = item;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }

    // Recebe golpes das pinças de Casco (inimigos, baús).
    public interface IHittable
    {
        void Hit(int damage, Vector2 from);
    }
}
