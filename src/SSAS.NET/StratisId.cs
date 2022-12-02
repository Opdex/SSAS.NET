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
        {
            if (callbackPath is null) throw new ArgumentNullException(nameof(callbackPath));
            if (redirectUri != null && !ValidateRedirectUri(redirectUri))
            {
                throw new ArgumentException("Redirect URI is not valid", nameof(redirectUri));
            }

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
        /// URI used to perform a redirect, on completion of SSAS mobile flow.
        /// </summary>
        public string RedirectUri { get; }

        private string BuildRedirectSchemeArgument() => RedirectUri?.Split(':')[0];

        private string BuildRedirectUriArgument()
        {
            if (RedirectUri is null) return null;
            var unserializedRedirectUriArgument = RedirectUri.Split(':')[1][2..];
            return unserializedRedirectUriArgument == ""
                ? null
                : WebUtility.UrlEncode(unserializedRedirectUriArgument);
        }

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
            
            var redirectSchemeArgument = BuildRedirectSchemeArgument();
            if (redirectSchemeArgument != null)protocolUriBuilder.Append('&').Append(RedirectSchemeKey).Append('=').Append(redirectSchemeArgument);
            
            var redirectUriArgument = BuildRedirectUriArgument();
            if (redirectUriArgument != null) protocolUriBuilder.Append('&').Append(RedirectUriKey).Append('=').Append(redirectUriArgument);
            
            return protocolUriBuilder.ToString();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return RedirectUri is null
                    ? HashCode.Combine(Callback.GetHashCode())
                    : HashCode.Combine(Callback.GetHashCode(), RedirectUri.GetHashCode());
        }

        /// <inheritdoc />
        public bool Equals(StratisId other) =>
            other is { }
            && Callback.Equals(other.Callback, StringComparison.InvariantCultureIgnoreCase)
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

            var redirectSchemeArgument = queryParams.ContainsKey(RedirectSchemeKey)
                ? queryParams[RedirectSchemeKey]
                : null;
            var redirectUriArgument = queryParams.ContainsKey(RedirectUriKey) 
                ? WebUtility.UrlDecode(queryParams[RedirectUriKey])
                : null;
            
            // validate redirect scheme and uri
            if ((redirectSchemeArgument is null && redirectUriArgument != null) || redirectSchemeArgument == "") return false;

            stratisId = new StratisId(callbackParts[0], queryParams[UidKey], exp, BuildRedirectUri(redirectSchemeArgument, redirectUriArgument));

            return true;
        }

        private static string BuildRedirectUri(string redirectScheme, string redirectUri)
        {
            if (redirectScheme is null) return null;
            var schemeWithAuthority = redirectScheme + "://";
            return redirectUri is null ? schemeWithAuthority : schemeWithAuthority + redirectUri;
        }

        private static bool ValidateRedirectUri(string redirectUri)
        {
            var uriParts = redirectUri.Split(':');
            return uriParts.Length == 2 && uriParts[0] != "";
        }
    }
}