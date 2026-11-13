using UnityEngine;

namespace MareBaixa
{
    // Vai no objeto do AudioListener de cada cena durante a gravação da demo: copia a mistura final do som
    // (tudo o que sai do alto-falante) para o DemoGravador. Roda na thread de áudio.
    public class DemoSom : MonoBehaviour
    {
        void OnAudioFilterRead(float[] data, int channels) => DemoGravador.ReceberSom(data, channels);
    }
}
