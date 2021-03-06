﻿using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Notify.Exceptions;
using System;
using System.Collections.Generic;

namespace Notify.Authentication
{
    public class Authenticator
    {
        public static String CreateToken(String secret, String serviceId)
        {
            ValidateGuids(new String[] { secret, serviceId });

            var payload = new Dictionary<String, object>()
            {
                { "iss", serviceId },
                { "iat", GetCurrentTimeAsSeconds() }
            };
            var algorithm = new HMACSHA256Algorithm();
            var serializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            String notifyToken = encoder.Encode(payload, secret);
            return notifyToken;
        }

        public static Double GetCurrentTimeAsSeconds()
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Math.Round((DateTime.UtcNow - unixEpoch).TotalSeconds);
        }

        public static IDictionary<String, Object> DecodeToken(String token, String secret)
        {
            try
            {
                var serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                var validator = new JwtValidator(serializer, provider);
                var urlEncoder = new JwtBase64UrlEncoder();
                var decoder = new JwtDecoder(serializer, validator, urlEncoder);

                var jsonPayload = decoder.DecodeToObject(token, secret,true) as IDictionary<String, Object>;
                return jsonPayload;
            }
            catch (Exception e) when (e is JWT.SignatureVerificationException || e is ArgumentException)
            {
                throw new NotifyAuthException(e.Message);
            } 
            catch(Exception e)
            {
                throw e;
            }
        }

        public static void ValidateGuids(String[] stringGuids)
        {
            Guid newGuid;
            if (stringGuids != null)
            {
                foreach (var stringGuid in stringGuids)
                {
                    if (!Guid.TryParse(stringGuid, out newGuid))
                        throw new NotifyAuthException("Invalid secret or serviceId. Please check that your API Key is correct");
                }
            }
        }
    }
    
}
