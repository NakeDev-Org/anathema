using UnityEngine;

namespace NakeDev.Attributes
{
    /// <summary>
    /// Mostra o campo no Inspector, mas desabilitado — para valores computados/debug que
    /// precisam ser visíveis sem serem editáveis (ex.: estado atual, contadores em runtime).
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
