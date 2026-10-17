namespace MareBaixa
{
    // Uma linha da loja: o texto à esquerda, o detalhe à direita (preço, nível) e o que acontece ao
    // confirmar. onSelect devolve true se deu certo (som de compra) ou false (som de erro).
    public class ShopEntry
    {
        public string label;
        public System.Func<string> detail;
        public System.Func<bool> onSelect;
    }
}
