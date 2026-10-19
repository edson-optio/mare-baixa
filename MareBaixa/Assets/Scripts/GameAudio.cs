using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    public enum Sfx { Jump, Attack, Hit, Crack, ShellBreak, Pearl, Bubble, Hurt, Death, Door, Buy, Error, Dodge, Spin, Shock, Burst, Equip, Eat, Charge, Flash, Rumble, Roar, Valve, Victory, Steal }

    // Efeitos sintetizados no código, como no lab05. Quando a cabeça de Casco fica debaixo
    // d'água, um filtro passa-baixa abafa todo o som (GDD p. 10).
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public class GameAudio : MonoBehaviour
    {
        AudioSource source;
        AudioSource music;
        AudioLowPassFilter lowPass;
        Zone? musicZone;
        static readonly Dictionary<Zone, AudioClip> songs = new Dictionary<Zone, AudioClip>();
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        float targetCutoff = 22000f;

        public bool Muffled => targetCutoff < 5000f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .3f;
            // A música fica no mesmo objeto para o filtro abafar tudo junto debaixo d'água.
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = .16f;
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
            clips[Sfx.Charge] = Noise(.3f, .3f, 500f);
            clips[Sfx.Flash] = Tone(1200f, 2400f, .35f, .18f);
            clips[Sfx.Rumble] = Noise(.6f, .35f, 180f);
            clips[Sfx.Roar] = Tone(140f, 55f, .8f, .35f);
            clips[Sfx.Valve] = Tone(200f, 120f, .5f, .25f);
            clips[Sfx.Victory] = Tone(523f, 1046f, .7f, .25f);
            clips[Sfx.Steal] = Tone(700f, 300f, .2f, .25f);
        }

        void Update()
        {
            lowPass.cutoffFrequency = Mathf.MoveTowards(lowPass.cutoffFrequency, targetCutoff, 60000f * Time.unscaledDeltaTime);
        }

        public void Play(Sfx sfx) => source.PlayOneShot(clips[sfx]);

        public void SetMuffled(bool muffled) => targetCutoff = muffled ? 750f : 22000f;

        // ---------------------------------------------------------------- Música

        // Calma e praiana na vila, cada vez mais grave e tensa nas zonas fundas (GDD p. 10).
        public void PlayMusic(Zone zone)
        {
            if (musicZone == zone && music.isPlaying) return;
            musicZone = zone;
            if (!songs.TryGetValue(zone, out AudioClip song)) songs[zone] = song = Compose(zone);
            music.clip = song;
            music.Play();
        }

        public void StopMusic()
        {
            musicZone = null;
            music.Stop();
        }

        readonly struct Style
        {
            public readonly float Bpm, Root, Lead, Pad;
            public readonly int[] Scale, Melody, Bass;
            public Style(float bpm, float root, int[] scale, int[] melody, int[] bass, float lead, float pad)
            {
                Bpm = bpm; Root = root; Scale = scale; Melody = melody; Bass = bass; Lead = lead; Pad = pad;
            }
        }

        static readonly int[] Major = { 0, 2, 4, 7, 9, 12, 14, 16 };
        static readonly int[] Minor = { 0, 3, 5, 7, 10, 12, 15, 17 };
        static readonly int[] Dark = { 0, 1, 5, 6, 10, 12, 13, 17 };

        // Melodia em colcheias (índice na escala, -1 = pausa) e baixo em semínimas (semitons da tônica).
        static Style StyleOf(Zone zone) => zone switch
        {
            Zone.Vila => new Style(100f, 261.6f, Major,
                new[] { 0, 2, 4, 2, 3, -1, 4, 5, 4, 3, 2, -1, 1, 2, 0, -1, 0, 2, 4, 5, 6, 5, 4, -1, 3, 2, 1, 2, 0, -1, -1, -1 },
                new[] { 0, 0, 7, 7, 5, 5, 7, 7 }, .5f, .25f),
            Zone.Recife => new Style(96f, 220f, Major,
                new[] { 4, -1, 3, 2, 3, -1, 1, -1, 2, 3, 4, 5, 4, -1, -1, -1, 2, -1, 1, 0, 1, -1, 2, -1, 3, 2, 1, 0, 1, -1, -1, -1 },
                new[] { 0, 0, 9, 9, 5, 5, 7, 7 }, .4f, .4f),
            Zone.Bolha => new Style(70f, 293.7f, Major,
                new[] { 0, -1, -1, 2, -1, -1, 4, -1, -1, -1, 3, -1, 2, -1, -1, -1, 1, -1, -1, 2, -1, -1, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 0, 5, 5, 7, 7, 0, 0 }, .25f, .5f),
            Zone.Cidade => new Style(88f, 174.6f, Minor,
                new[] { 0, -1, 0, 2, 3, -1, 2, -1, 0, -1, 4, 3, 2, -1, -1, -1, 0, -1, 0, 2, 3, -1, 5, 4, 3, -1, 2, -1, 1, -1, -1, -1 },
                new[] { 0, 0, 0, 0, 8, 8, 7, 7 }, .35f, .45f),
            _ => new Style(66f, 110f, Dark,
                new[] { 0, -1, -1, -1, 1, -1, -1, -1, 0, -1, -1, 3, -1, -1, -1, -1, 2, -1, -1, -1, 1, -1, -1, -1, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 0, 0, 0, 1, 1, 0, 0 }, .3f, .6f),
        };

        // Dois compassos repetidos 4 vezes, com o baixo e um acorde de fundo. Sintetizado na hora, uma vez.
        static AudioClip Compose(Zone zone)
        {
            Style style = StyleOf(zone);
            const int rate = 22050;
            float eighth = 30f / style.Bpm;
            int stepSamples = Mathf.RoundToInt(eighth * rate);
            int steps = style.Melody.Length * 2;   // a frase toca duas vezes por loop
            var samples = new float[stepSamples * steps];
            float leadPhase = 0f, bassPhase = 0f, padPhase = 0f;
            for (int step = 0; step < steps; step++)
            {
                int note = style.Melody[step % style.Melody.Length];
                int bassNote = style.Bass[(step / 4) % style.Bass.Length];
                float lead = note >= 0 ? style.Root * 2f * Mathf.Pow(2f, style.Scale[note] / 12f) : 0f;
                float bass = style.Root * .5f * Mathf.Pow(2f, bassNote / 12f);
                float pad = style.Root * Mathf.Pow(2f, (bassNote + style.Scale[2]) / 12f);
                for (int i = 0; i < stepSamples; i++)
                {
                    float t = i / (float)stepSamples;
                    float value = 0f;
                    if (lead > 0f)
                    {
                        leadPhase += Mathf.PI * 2f * lead / rate;
                        float tone = Mathf.Clamp(Mathf.Sin(leadPhase) * 2f, -1f, 1f);
                        value += tone * Mathf.Min(1f, t * 40f) * Mathf.Pow(1f - t, 1.5f) * style.Lead;
                    }
                    bassPhase += Mathf.PI * 2f * bass / rate;
                    value += Mathf.Sin(bassPhase) * (.55f + .45f * (1f - (step % 2 == 0 ? t : 1f))) * .5f;
                    padPhase += Mathf.PI * 2f * pad / rate;
                    value += Mathf.Sin(padPhase) * style.Pad * .35f;
                    samples[step * stepSamples + i] = value * .45f;
                }
            }
            var clip = AudioClip.Create("musica-" + zone, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

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
