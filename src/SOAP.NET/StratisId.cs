using System;
using System.Collections.Generic;
using System.Text;

namespace SOAP.NET
{
    /// <summary>
    /// A Stratis ID URI defined in Stratis Open Auth Protocol.
    /// </summary>
    public class StratisId : IEquatable<StratisId>
    {
        /// <summary>
        /// Stratis ID URI scheme.
        /// </summary>
        public const string Scheme = "sid";

        private const string UidKey = "uid";
        private const string ExpKey = "exp";
        private const string SchemeWithDelimeter = "sid:";

        private readonly DateTime _expiry = DateTime.MaxValue;

        /// <summary>
        /// Constructs a Stratis ID URI from its parts.
        /// </summary>
        /// <param name="callbackPath">The combined domain and path of the callback URL.</param>
        /// <param name="uid">The unique identifier for a request.</param>
        /// <param name="exp">A unix timestamp that specifies when the signature should expire.</param>
        public StratisId(string callbackPath, string uid, long? exp = null)
        {
            if (callbackPath is null) throw new ArgumentNullException(nameof(callbackPath));
            if (uid is null) throw new ArgumentNullException(nameof(callbackPath));

            Uid = uid;

            var callbackBuilder = new StringBuilder();
            callbackBuilder.Append(callbackPath).Append('?').Append(UidKey).Append('=').Append(uid);

            if (exp.HasValue)
            {
                _expiry = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime;
                callbackBuilder.Append('&').Append(ExpKey).Append('=').Append(exp.Value.ToString());
            }

            Callback = callbackBuilder.ToString();
        }

        /// <summary>
        /// Protocol-relative callback URL to which the signer sends a HTTPS request.
        /// </summary>
        public string Callback { get; }

        /// <summary>
        /// A unique identifier for a request.
        /// </summary>
        public string Uid { get; }

        /// <summary>
        /// Returns true if the Stratis ID URI is expired relative to the current UTC time; otherwise false.
        /// </summary>
        public bool Expired => DateTime.UtcNow > _expiry;

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> URI representation, including the scheme.
        /// </summary>
        public override string ToString() => $"{SchemeWithDelimeter}{Callback}";

        /// <inheritdoc />
        public override int GetHashCode() => Callback.GetHashCode();

        /// <inheritdoc />
        public bool Equals(StratisId other) => Callback.Equals(other.Callback, StringComparison.InvariantCultureIgnoreCase);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is StratisId other && Equals(other);

        /// <inheritdoc />
        public static bool operator ==(StratisId a, StratisId b) => a.Equals(b);

        /// <inheritdoc />
        public static bool operator !=(StratisId a, StratisId b) => !a.Equals(b);

        /// <summary>
        /// Converts the string representation of a Stratis ID URI or callback to its <see cref="StratisId" /> equivalent. A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="value">A string containing the Stratis ID URI or callback to convert.</param>
        /// <param name="stratisId">When this method returns true, contains the <see cref="StratisId" /> equivalent of the value, or null if the conversion failed.</param>
        /// <returns>true if value was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string value, out StratisId stratisId)
        {
            stratisId = null;

            var callback = value.StartsWith(SchemeWithDelimeter) ? value.Substring(SchemeWithDelimeter.Length) : value;
            if (callback.StartsWith("//")) return false;

            var callbackParts = callback.Split('?');
            if (callbackParts.Length != 2) return false;

            var queryString = callbackParts[1];
            if (queryString is null) return false;

            var queryParams = new Dictionary<string, string>();

            var queryParts = queryString.Split('&');
            foreach (var part in queryParts)
            {
                var equalSignIndex = part.IndexOf('=');
                if (equalSignIndex == -1) continue;

                queryParams.Add(part.Substring(0, equalSignIndex), part.Substring(equalSignIndex + 1));
            }

            if (!queryParams.ContainsKey(UidKey) || string.IsNullOrWhiteSpace(queryParams[UidKey])) return false;

            long? exp = null;
            if (queryParams.ContainsKey(ExpKey))
            {
                if (!long.TryParse(queryParams[ExpKey], out var expiry)) return false;
                exp = expiry;
            }

            stratisId = new StratisId(callbackParts[0], queryParams[UidKey], exp);

            return true;
        }
    }
}