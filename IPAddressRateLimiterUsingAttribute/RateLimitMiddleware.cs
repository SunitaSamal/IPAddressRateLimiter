using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IPAddressRateLimiterUsingAttribute
{ 
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        static readonly ConcurrentDictionary<string, DateTime?> _currentRequests = new ConcurrentDictionary<string, DateTime?>();
        public RateLimitMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

            if (controllerActionDescriptor is null)
            {
                await _next(context);
                return;
            }

            var rateLimitProperty = (RateLimitAttribute)controllerActionDescriptor.MethodInfo
                            .GetCustomAttributes(true)
                            .SingleOrDefault(w => w.GetType() == typeof(RateLimitAttribute));

            if (rateLimitProperty is null)
            {
                await _next(context);
                return;
            }

            string clientRequestKey = GetCurrentClientKey(rateLimitProperty, context);

            var previousClientRequest = GetCurrentClientRequestByKey(clientRequestKey);
            if (previousClientRequest != null)
            {

                if (DateTime.Now < previousClientRequest.Value.AddSeconds(5))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    return;
                }
            }

            UpdateClientRequest(clientRequestKey);

            await _next(context);
        }

        /// <summary>
        /// Keep track of client request date and time
        /// </summary>
        /// <param name="key"></param>
        private void UpdateClientRequest(string key)
        {
            _currentRequests.TryRemove(key, out _);
            _currentRequests.TryAdd(key, DateTime.Now);
        }

        private DateTime? GetCurrentClientRequestByKey(string key)
        {
            _currentRequests.TryGetValue(key, out DateTime? value);
            return value;
        }

        /// <summary>
        /// Gets ClientRequest IP address as key
        /// </summary>
        /// <param name="rateLimitProperty"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string GetCurrentClientKey(RateLimitAttribute rateLimitProperty, HttpContext context)
        {
            var requestKeysList = new List<string>
            {
                context.Request.Path
            };

            string clientIpAddress = GetClientIpAddress(context);
            if (!requestKeysList.Contains(clientIpAddress))
                requestKeysList.Add(clientIpAddress);
             
            return string.Join('_', requestKeysList);
        }

        /// <summary>
        /// Returns the client's Ip Address from HttpContent
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string GetClientIpAddress(HttpContext context)
        { 
            return context.Connection.RemoteIpAddress.ToString();
        }
    }
}
