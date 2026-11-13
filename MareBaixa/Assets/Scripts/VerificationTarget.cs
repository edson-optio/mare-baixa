using UnityEngine;

namespace MareBaixa
{
    // Alvo criado exclusivamente por -selfTest para medir acertos reais das caixas de combate.
    public class VerificationTarget : MonoBehaviour, IHittable, IStunnable
    {
        public int Damage { get; private set; }
        public int Hits { get; private set; }
        public float StunTime { get; private set; }
        public void Hit(int damage, Vector2 from) { Damage += damage; Hits++; }
        public void Stun(float seconds) => StunTime = seconds;

        public static VerificationTarget Spawn(Vector2 position, Transform parent)
        {
            var go = new GameObject("Alvo de verificação", typeof(BoxCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(.2f, .5f);
            return go.AddComponent<VerificationTarget>();
        }
    }
}
