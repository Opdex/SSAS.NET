using System;
using System.Collections.Generic;
using System.Text;

namespace SSAS.NET
{
    /// <summary>
    /// A Stratis ID URI defined in Stratis Signature Auth Specification.
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
        private const string ProtocolHandlerSchemeWithDelimeter = "web+sid://";

        /// <summary>
        /// Constructs a Stratis ID URI from its parts.
        /// </summary>
        /// <param name="callbackPath">The combined authority and path of the callback URL.</param>
        /// <param name="uid">The unique identifier for a request.</param>
        /// <param name="exp">A unix timestamp that specifies when the signature should expire.</param>
        public StratisId(string callbackPath, string uid, long? exp = null)
        {
            if (callbackPath is null) throw new ArgumentNullException(nameof(callbackPath));

            Uid = uid ?? throw new ArgumentNullException(nameof(uid));

            // parse callback path correctly if scheme or authority indicator is present
            callbackPath = callbackPath.StartsWith("https://") ? callbackPath[8..] : callbackPath.TrimStart('/');

            var callbackBuilder = new StringBuilder();
            callbackBuilder.Append(callbackPath).Append('?').Append(UidKey).Append('=').Append(uid);

            if (exp.HasValue)
            {
                Expiry = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime;
                callbackBuilder.Append('&').Append(ExpKey).Append('=').Append(exp.Value.ToString());
            }

            Callback = callbackBuilder.ToString();
        }

        /// <summary>
        /// Constructs a Stratis ID URI from its parts.
        /// </summary>
        /// <param name="callbackPath">The combined authority and path of the callback URL.</param>
        /// <param name="uid">The unique identifier for a request.</param>
        /// <param name="exp">A timestamp that specifies when the signature should expire.</param>
        public StratisId(string callbackPath, string uid, DateTimeOffset exp) : this(callbackPath, uid, exp.ToUnixTimeSeconds())
        {
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
        /// UTC expiry time for the Stratis Id.
        /// </summary>
        public DateTime Expiry { get; } = DateTime.MaxValue;

        /// <summary>
        /// Returns true if the Stratis ID URI is expired relative to the current UTC time; otherwise false.
        /// </summary>
        public bool Expired => DateTime.UtcNow > Expiry;

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> representation.
        /// </summary>
        public override string ToString() => Callback;

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> URI representation, including the scheme.
        /// </summary>
        public string ToUriString()
        {
            return $"{SchemeWithDelimeter}{Callback}";
        }

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> protocol handler URI representation.
        /// </summary>
        public string ToProtocolString()
        {
            return $"{ProtocolHandlerSchemeWithDelimeter}{Callback}";
        }

        /// <inheritdoc />
        public override int GetHashCode() => Callback.GetHashCode();

        /// <inheritdoc />
        public bool Equals(StratisId other) => other is { } && Callback.Equals(other.Callback, StringComparison.InvariantCultureIgnoreCase);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is StratisId other && Equals(other);

        public static bool operator ==(StratisId a, StratisId b)
        {
            if (a is null) return b is null;
            return a.Equals(b);
        }

        public static bool operator !=(StratisId a, StratisId b) => !(a == b);

        /// <summary>
        /// Converts the string representation of a Stratis ID URI or callback to its <see cref="StratisId" /> equivalent. A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="value">A string containing the Stratis ID URI or callback to convert.</param>
        /// <param name="stratisId">When this method returns true, contains the <see cref="StratisId" /> equivalent of the value, or null if the conversion failed.</param>
        /// <returns>true if value was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string value, out StratisId stratisId)
        {
            stratisId = null;

            var callback = value.StartsWith(SchemeWithDelimeter)
                ? value[SchemeWithDelimeter.Length..]
                : value.StartsWith(ProtocolHandlerSchemeWithDelimeter)
                    ? value[ProtocolHandlerSchemeWithDelimeter.Length..]
                    : value;
            if (callback.StartsWith("//")) return false;

            var callbackParts = callback.Split('?', StringSplitOptions.RemoveEmptyEntries);
            if (callbackParts.Length != 2) return false;

            var queryString = callbackParts[1];

            var queryParams = new Dictionary<string, string>();

            var queryParts = queryString.Split('&');
            foreach (var part in queryParts)
            {
                var equalSignIndex = part.IndexOf('=');
                if (equalSignIndex == -1) continue;

                queryParams.Add(part[..equalSignIndex], part[(equalSignIndex + 1)..]);
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