namespace MareBaixa
{
    // A concha é classe e vida ao mesmo tempo. Lata e Lâmpada (Cidade Afundada e Fossa)
    // entram quando essas zonas forem feitas; a tabela do GDD p. 7 já prevê os valores.
    public enum ShellType { Nenhuma, Comum, Tampinha, Espiral }

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
            new ShellInfo("Concha Espiral", 4, 1f, "Ataque giratório dos dois lados"),
        };

        public static ShellInfo Get(ShellType type) => Table[(int)type];
    }
}
