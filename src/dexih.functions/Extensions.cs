﻿using System;
using System.Collections.Generic;
using System.Text;

namespace dexih.functions
{
    public static class Extensions
    {
        
        /// <summary>
        /// Use to get a value or a default from a dictionary.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public static TV GetValueOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV))
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Checks if an object is null or a blank string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ObjectIsNullOrBlank(this object value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is string s && string.IsNullOrWhiteSpace(s))
            {
                return true;
            }

            return false;
        }
        
        public static bool IsPattern(this string value, string pattern)
        {
            if (value.Length != pattern.Length) return false;
            for (var i = 0; i < pattern.Length; i++)
            {
                if ((pattern[i] == '9' && !char.IsNumber(value[i])) ||
                    (pattern[i] == 'A' && !char.IsUpper(value[i])) ||
                    (pattern[i] == 'a' && !char.IsLower(value[i])) ||
                    (pattern[i] == 'Z' && !char.IsLetter(value[i])))
                    return false;
            }

            return true;
        }
        
        public static string CreateSHA1(this string value)
        {
            if (value == null)
            {
                return null;
            }
            
            var bytes = Encoding.UTF8.GetBytes(value);
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var hash = sha1.ComputeHash(bytes);
                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                var sBuilder = new StringBuilder();

                for (var i = 0; i < hash.Length; i++)
                {
                    sBuilder.Append(hash[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
        
        public static DateTime UnixTimeStampToDate(this long unixTimeStamp)
        {
            var origDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var convertedDate = origDate.AddSeconds(unixTimeStamp).ToLocalTime();
            return convertedDate;
        }
    }
}