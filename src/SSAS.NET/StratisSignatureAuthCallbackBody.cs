using System.ComponentModel.DataAnnotations;

namespace SSAS.NET
{
    /// <summary>
    /// Callback body for Stratis Signature Auth Specification.
    /// </summary>
    public class StratisSignatureAuthCallbackBody
    {
        /// <summary>
        /// Signed Stratis ID callback.
        /// </summary>
        /// <example>H9xjfnvqucCmi3sfEKUes0qL4mD9PrZ/al78+Ka440t6WH5Qh0AIgl5YlxPa2cyuXdwwDa2OYUWR/0ocL6jRZLc=</example>
        [Required]
        public string Signature { get; set; }

        /// <summary>
        /// Message signer wallet address.
        /// </summary>
        /// <example>tQ9RukZsB6bBsenHnGSo1q69CJzWGnxohm</example>
        [Required]
        public string PublicKey { get; set; }
    }
}