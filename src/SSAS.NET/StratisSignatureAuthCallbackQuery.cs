using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SSAS.NET
{
    /// <summary>
    /// Callback query parameters for Stratis Signature Auth Specification.
    /// </summary>
    public class StratisSignatureAuthCallbackQuery
    {
        /// <summary>
        /// The unique identifier of the Stratis ID.
        /// </summary>
        /// <example>4e8a8445762c491fa7c5cf74a0a745e5</example>
        [BindRequired]
        public string Uid { get; set; }

        /// <summary>
        ///  Unix timestamp indicating when the signature expires.
        /// </summary>
        /// <example>1637244295</example>
        public long Exp { get; set; }
    }
}