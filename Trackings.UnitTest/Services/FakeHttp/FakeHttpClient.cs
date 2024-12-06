﻿using System;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Trackings.UnitTest.Services.FakeHttp
{
    public static class FakeHttpClient
    {
        public static HttpClient GetFakeClient(string apiHeader, string apiToken, HttpStatusCode statusCode, object expected)
        {
            var fakeHttpMessageHandler = new FakeHttpMessageHandler(apiHeader, apiToken, new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json")
            });
            var fakeClient = new HttpClient(fakeHttpMessageHandler);
            fakeClient.BaseAddress = new Uri("http://www.wiley-epic.com");

            return fakeClient;
        }

        public static HttpClient GetFakeClientSimple(HttpStatusCode statusCode, string expected)
        {
            var fakeHttpMessageHandler = new FakeHttpMessageHandlerSimple(new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(expected, Encoding.UTF8, "application/json")
            });
            var fakeClient = new HttpClient(fakeHttpMessageHandler);
            fakeClient.BaseAddress = new Uri("http://www.wiley-epic.com");

            return fakeClient;
        }
    }
}
