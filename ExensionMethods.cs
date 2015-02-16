namespace Adfitech
{
    using System;
    using System.Net;
    using System.Security.Cryptography;

    // courtesy of @ajperrins - https://gist.github.com/ajperrins/0f60a58e9898e8868688
    // with minor revisions.
    internal static class Extensions
    {
        /// <summary>
        ///     Returns a Date as a string, metting HTTP Date header format requirements (check RFC)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string HttpDate(this DateTime date)
        {
            return date.ToUniversalTime().ToString("r");
        }

        /// <summary>
        ///     String (utf8 encoded) as byte array
        /// </summary>
        /// <param name="utfStr"></param>
        /// <returns></returns>
        public static byte[] Bytes(this string utfStr)
        {
            return System.Text.Encoding.UTF8.GetBytes(utfStr);
        }

        /// <summary>
        ///     Applies the HMAC authorization header, and ensures the Date header matches that used in the
        ///     hash payload. See the Apiary API for more information and HMAC pointers
        /// </summary>
        /// <param name="request">HttpWebRequest</param>
        /// <param name="key">The api key provided by adfitech.</param>
        /// <param name="key_id">The api key id provided by adfitech.</param>
        public static void AddAuthorizationHeader(this HttpWebRequest request, string key, int key_id)
        {
            // Set up hash payload
            var now = DateTime.Now;
            var canonicalString = string.Format("{0}\n{1}\n{2}\n{3}", request.Method,
                                                request.ContentType, now.HttpDate(), 
                                                request.RequestUri.AbsolutePath);
            // Hash calculation
            var hmacSha = new HMACSHA1(key.Bytes());
            var hash = hmacSha.ComputeHash(canonicalString.Bytes());
            var hmac = Convert.ToBase64String(hash);

            // Set request headers
            request.Headers.Add("Authorization: " + string.Format("AD {0}:{1}", key_id, hmac));
            request.Date = now;
        }
    }
}
