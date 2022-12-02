using System;
using System.Collections.Generic;
using System.Net;
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

        /// <summary>
        /// Protocol handler compatible Stratis ID URI scheme.
        /// </summary>
        public const string ProtocolScheme = "web+sid";

        private const string UidKey = "uid";
        private const string ExpKey = "exp";
        private const string RedirectSchemeKey = "redirectScheme";
        private const string RedirectUriKey = "redirectUri";

        private const string SchemeWithDelimeter = "sid:";
        private const string ProtocolHandlerSchemeWithDelimeter = "web+sid:";

        /// <summary>
        /// Constructs a Stratis ID URI from its parts.
        /// </summary>
        /// <param name="callbackPath">The combined authority and path of the callback URL.</param>
        /// <param name="uid">The unique identifier for a request.</param>
        /// <param name="exp">A timestamp that specifies when the signature should expire.</param>
        /// <param name="redirectUri">The full URI to redirect the user on completion of the mobile flow.</param>
        public StratisId(string callbackPath, string uid, DateTimeOffset exp, string redirectUri = null)
            : this(callbackPath, uid, exp.ToUnixTimeSeconds(), redirectUri)
        {
        }

        /// <summary>
        /// Constructs a Stratis ID URI from its parts.
        /// </summary>
        /// <param name="callbackPath">The combined authority and path of the callback URL.</param>
        /// <param name="uid">The unique identifier for a request.</param>
        /// <param name="exp">A unix timestamp that specifies when the signature should expire.</param>
        /// <param name="redirectUri">The full URI to redirect the user on completion of the mobile flow.</param>
        public StratisId(string callbackPath, string uid, long? exp = null, string redirectUri = null)
            : this(callbackPath, uid, exp, ParseUriScheme(redirectUri), ParseUriAfterScheme(redirectUri))
        {
        }

        private static string ParseUriScheme(string redirectUri)
        {
            if (redirectUri is null) return null;
            var parts = redirectUri.Split(':');
            if (parts.Length != 2) throw new ArgumentException("Redirect URI must be a valid URI", nameof(redirectUri));
            if (string.IsNullOrWhiteSpace(parts[0])) throw new ArgumentException("Redirect URI must contain a valid scheme", nameof(redirectUri));
            return parts[0];
        }

        private static string ParseUriAfterScheme(string redirectUri)
        {
            if (redirectUri is null) return null;
            var parts = redirectUri.Split(':');
            return parts[1] == "" || parts[1] == "//"
                ? null
                : parts[1].StartsWith("//")
                    ? parts[1][2..]
                    : parts[1];
        }

        private StratisId(string callbackPath, string uid, long? exp = null,
                          string redirectScheme = null, string redirectUri = null)
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

            RedirectScheme = redirectScheme;
            RedirectUri = redirectUri;
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
        /// URI scheme used to perform a redirect, on completion of SSAS mobile flow.
        /// </summary>
        public string RedirectScheme { get; }

        /// <summary>
        /// Schemeless URI used in redirect, on completion of SSAS mobile flow.
        /// </summary>
        public string RedirectUri { get; }

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> representation.
        /// </summary>
        public override string ToString() => Callback;

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> URI representation, including the scheme.
        /// </summary>
        public string ToUriString() => $"{SchemeWithDelimeter}{Callback}";

        /// <summary>
        /// Converts the <see cref="StratisId" /> to its <see cref="string" /> protocol handler URI representation.
        /// </summary>
        public string ToProtocolString()
        {
            var protocolUriBuilder = new StringBuilder();
            protocolUriBuilder.Append(ProtocolHandlerSchemeWithDelimeter).Append(Callback);
            if (RedirectScheme != null) protocolUriBuilder.Append('&').Append(RedirectSchemeKey).Append('=').Append(RedirectScheme);
            if (RedirectUri != null) protocolUriBuilder.Append('&').Append(RedirectUriKey).Append('=').Append(WebUtility.UrlEncode(RedirectUri));
            
            return protocolUriBuilder.ToString();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return RedirectScheme is null
                ? Callback.GetHashCode()
                : RedirectUri is null
                    ? HashCode.Combine(Callback.GetHashCode(), RedirectScheme.GetHashCode())
                    : HashCode.Combine(Callback.GetHashCode(), RedirectScheme.GetHashCode(), RedirectUri.GetHashCode());
        }

        /// <inheritdoc />
        public bool Equals(StratisId other) =>
            other is { }
            && Callback.Equals(other.Callback, StringComparison.InvariantCultureIgnoreCase)
            && ((RedirectScheme is null && other.RedirectScheme is null) || (RedirectScheme != null && RedirectScheme.Equals(other.RedirectScheme, StringComparison.InvariantCultureIgnoreCase)))
            && ((RedirectUri is null && other.RedirectUri is null) || (RedirectUri != null && RedirectUri.Equals(other.RedirectUri, StringComparison.InvariantCultureIgnoreCase)));

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
            if (callback.StartsWith("//")) callback = callback[2..];

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

            var redirectScheme = queryParams.ContainsKey(RedirectSchemeKey)
                ? queryParams[RedirectSchemeKey]
                : null;
            var redirectUri = queryParams.ContainsKey(RedirectUriKey) 
                ? WebUtility.UrlDecode(queryParams[RedirectUriKey])
                : null;
            
            // validate redirect scheme and uri
            if ((redirectScheme is null && redirectUri != null) || redirectScheme == "") return false;

            stratisId = new StratisId(callbackParts[0], queryParams[UidKey], exp, redirectScheme, redirectUri);

            return true;
        }
    }
}