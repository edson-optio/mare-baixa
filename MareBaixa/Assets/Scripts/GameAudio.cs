using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    public enum Sfx { Jump, Attack, Hit, Crack, ShellBreak, Pearl, Bubble, Hurt, Death, Door, Buy, Error, Dodge, Spin, Shock, Burst, Equip, Eat }

    // Efeitos sintetizados no código, como no lab05. Quando a cabeça de Casco fica debaixo
    // d'água, um filtro passa-baixa abafa todo o som (GDD p. 10).
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public class GameAudio : MonoBehaviour
    {
        AudioSource source;
        AudioLowPassFilter lowPass;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        float targetCutoff = 22000f;

        public bool Muffled => targetCutoff < 5000f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .3f;
            lowPass = GetComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = targetCutoff;

            clips[Sfx.Jump] = Tone(330f, 620f, .11f, .2f);
            clips[Sfx.Attack] = Noise(.08f, .18f, 2500f);
            clips[Sfx.Hit] = Tone(260f, 110f, .09f, .32f);
            clips[Sfx.Crack] = Noise(.16f, .35f, 1200f);
            clips[Sfx.ShellBreak] = Noise(.45f, .4f, 700f);
            clips[Sfx.Pearl] = Tone(990f, 1480f, .09f, .22f);
            clips[Sfx.Bubble] = Tone(420f, 1100f, .18f, .22f);
            clips[Sfx.Hurt] = Tone(220f, 80f, .25f, .32f);
            clips[Sfx.Death] = Tone(320f, 60f, .9f, .3f);
            clips[Sfx.Door] = Tone(180f, 360f, .3f, .25f);
            clips[Sfx.Buy] = Tone(660f, 1320f, .25f, .22f);
            clips[Sfx.Error] = Tone(160f, 140f, .18f, .25f);
            clips[Sfx.Dodge] = Noise(.15f, .12f, 900f);
            clips[Sfx.Spin] = Tone(300f, 900f, .25f, .2f);
            clips[Sfx.Shock] = Noise(.25f, .22f, 6000f);
            clips[Sfx.Burst] = Noise(.22f, .32f, 2000f);
            clips[Sfx.Equip] = Tone(520f, 780f, .16f, .22f);
            clips[Sfx.Eat] = Tone(400f, 600f, .2f, .2f);
        }

        void Update()
        {
            lowPass.cutoffFrequency = Mathf.MoveTowards(lowPass.cutoffFrequency, targetCutoff, 60000f * Time.unscaledDeltaTime);
        }

        public void Play(Sfx sfx) => source.PlayOneShot(clips[sfx]);

        public void SetMuffled(bool muffled) => targetCutoff = muffled ? 750f : 22000f;

        // Onda quadrada suave com frequência deslizando (som de "8 bits").
        static AudioClip Tone(float startFrequency, float endFrequency, float duration, float amplitude)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                phase += Mathf.PI * 2f * Mathf.Lerp(startFrequency, endFrequency, progress) / rate;
                float square = Mathf.Clamp(Mathf.Sin(phase) * 3f, -1f, 1f);
                samples[i] = square * Mathf.Min(1f, progress * 30f) * (1f - progress) * amplitude;
            }
            var clip = AudioClip.Create("tom", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // Ruído filtrado: rachaduras, golpes e choques.
        static AudioClip Noise(float duration, float amplitude, float brightness)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(rate * duration);
            var samples = new float[count];
            var random = new System.Random(count);
            float smoothing = Mathf.Clamp01(brightness / rate * 6f);
            float value = 0f;
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                value = Mathf.Lerp(value, (float)random.NextDouble() * 2f - 1f, smoothing);
                samples[i] = value * Mathf.Min(1f, progress * 40f) * (1f - progress) * amplitude * 2f;
            }
            var clip = AudioClip.Create("ruido", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
