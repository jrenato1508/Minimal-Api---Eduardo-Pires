namespace DemoMinimalAPI.Models
{
    public class Fornecedor
    {
        public Guid id { get; set; }

        public string? Nome { get; set; }

        public string? Documento { get; set; }

        public int TipoFornecedor { get; set; }

        public bool  Ativo { get; set; }
    }
}
