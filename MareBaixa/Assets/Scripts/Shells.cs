namespace MareBaixa
{
    // A concha é classe e vida ao mesmo tempo (tabela do GDD p. 7). A ordem não muda: o save guarda o índice.
    public enum ShellType { Nenhuma, Comum, Tampinha, Espiral, Lata, Lampada }

    public readonly struct ShellInfo
    {
        public readonly string Name;
        public readonly int Resistance;
        public readonly float SpeedFactor;
        public readonly string Ability;

        public ShellInfo(string name, int resistance, float speedFactor, string ability)
        {
            Name = name;
            Resistance = resistance;
            SpeedFactor = speedFactor;
            Ability = ability;
        }
    }

    public static class Shells
    {
        static readonly ShellInfo[] Table =
        {
            new ShellInfo("Sem concha", 0, GameRules.NakedSpeedFactor, "Um golpe e acabou"),
            new ShellInfo("Concha Comum", 3, 1f, "Nenhuma habilidade"),
            new ShellInfo("Tampinha de Garrafa", 2, 1.2f, "Pulo duplo"),
            new ShellInfo("Concha Espiral", 4, 1f, "Cambalhota ofensiva"),
            new ShellInfo("Lata de Refrigerante", 6, .8f, "Investida que derruba"),
            new ShellInfo("Lâmpada Velha", 3, 1f, "Clarão que atordoa"),
        };

        // Conchas novas que a sala sorteia com "r" em cada zona (GDD p. 7: onde aparece).
        public static ShellType RandomFor(Zone zone)
        {
            bool deep = zone == Zone.Cidade || zone == Zone.Fossa;
            bool first = UnityEngine.Random.value < .5f;
            if (deep) return first ? ShellType.Lata : ShellType.Lampada;
            return first ? ShellType.Tampinha : ShellType.Espiral;
        }

        public static ShellInfo Get(ShellType type) => Table[(int)type];
    }
}
