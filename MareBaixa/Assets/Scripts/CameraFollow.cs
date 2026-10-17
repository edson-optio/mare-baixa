using UnityEngine;

namespace MareBaixa
{
    // Câmera que segue Casco sem mostrar o lado de fora da sala e treme nos golpes fortes.
    [DefaultExecutionOrder(-50)] // move antes do Parallax ler a posição dela
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = .12f;
        public Vector2 lookAhead = new Vector2(1.2f, .8f);
        public float autoScrollSpeed;

        Vector2 min = new Vector2(-100000f, -100000f);
        Vector2 max = new Vector2(100000f, 100000f);
        Vector3 velocity;
        Vector3 basePosition;
        float shakeUntil;
        float shakeStrength;

        void Awake() => basePosition = transform.position;

        public void SetBounds(float roomWidth, float roomHeight)
        {
            float halfHeight = GameRules.CameraSize;
            float halfWidth = halfHeight * GameRules.TargetAspect;
            // Sala menor que a tela fica centralizada.
            min = new Vector2(Mathf.Min(halfWidth, roomWidth * .5f), Mathf.Min(halfHeight, roomHeight * .5f));
            max = new Vector2(Mathf.Max(roomWidth - halfWidth, roomWidth * .5f), Mathf.Max(roomHeight - halfHeight, roomHeight * .5f));
        }

        public void Shake(float strength, float duration)
        {
            shakeStrength = Mathf.Max(strength, Time.unscaledTime < shakeUntil ? shakeStrength : 0f);
            shakeUntil = Time.unscaledTime + duration;
        }

        void LateUpdate()
        {
            Vector3 position = basePosition;
            if (target != null)
            {
                float facing = Mathf.Sign(target.GetComponent<Casco>().Facing);
                Vector3 goal = target.position + new Vector3(lookAhead.x * facing, lookAhead.y, 0f);
                goal.z = position.z;
                position = Vector3.SmoothDamp(position, goal, ref velocity, smoothTime);
            }
            else
            {
                position.x += autoScrollSpeed * Time.deltaTime;
            }
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            basePosition = position;

            Vector3 shake = Vector3.zero;
            if (Time.unscaledTime < shakeUntil)
                shake = (Vector3)(Random.insideUnitCircle * shakeStrength);
            transform.position = basePosition + shake;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            basePosition = new Vector3(target.position.x, target.position.y, basePosition.z);
            LateUpdate();
        }
    }
}
