using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sentry.Unity
{
    public class SentryUnityWebRequest : UnityWebRequest
    {
        public SentryUnityWebRequest()
        {
        }

        public SentryUnityWebRequest(string url) :
            base(url)
        {
        }

        public SentryUnityWebRequest(Uri uri) :
            base(uri)
        {
        }

        public SentryUnityWebRequest(string url, string method) :
            base(url, method)
        {
        }

        public SentryUnityWebRequest(Uri uri, string method) :
            base(uri, method)
        {
        }

        public SentryUnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler? uploadHandler) :
            base(url, method, downloadHandler, uploadHandler)
        {
        }

        public SentryUnityWebRequest(Uri uri, string method, DownloadHandler downloadHandler, UploadHandler? uploadHandler) :
            base(uri, method, downloadHandler, uploadHandler)
        {
        }

        public new static UnityWebRequest Get(string uri) =>
            new SentryUnityWebRequest(uri, "GET", new DownloadHandlerBuffer(), null);

        public new static UnityWebRequest Get(Uri uri)
            => new SentryUnityWebRequest(uri, "GET", new DownloadHandlerBuffer(), null);

        public new static UnityWebRequest Delete(string uri)
            => new SentryUnityWebRequest(uri, "DELETE");

        public new static UnityWebRequest Delete(Uri uri)
            => new SentryUnityWebRequest(uri, "DELETE");

        public new static UnityWebRequest Head(string uri)
            => new SentryUnityWebRequest(uri, "HEAD");

        public new static UnityWebRequest Head(Uri uri) => new SentryUnityWebRequest(uri, "HEAD");

        public new static UnityWebRequest Put(string uri, byte[] bodyData)
            => new SentryUnityWebRequest(uri, "PUT", new DownloadHandlerBuffer(), new UploadHandlerRaw(bodyData));

        public new static UnityWebRequest Put(Uri uri, byte[] bodyData)
            => new SentryUnityWebRequest(uri, "PUT", new DownloadHandlerBuffer(), new UploadHandlerRaw(bodyData));

        public new static UnityWebRequest Put(string uri, string bodyData)
            => new SentryUnityWebRequest(uri, "PUT", new DownloadHandlerBuffer(), new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyData)));

        public new static UnityWebRequest Put(Uri uri, string bodyData)
            => new SentryUnityWebRequest(uri, "PUT", new DownloadHandlerBuffer(), new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyData)));

        public new static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections)
            => Post(uri, multipartFormSections, GenerateBoundary());

        public new static UnityWebRequest Post(Uri uri, List<IMultipartFormSection> multipartFormSections)
            => Post(uri, multipartFormSections, GenerateBoundary());

        public new static UnityWebRequest Post(string uri, string postData)
        {
            using var unityRequest = UnityWebRequest.Post(uri, postData);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(Uri uri, string postData)
        {
            using var unityRequest = UnityWebRequest.Post(uri, postData);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(string uri, WWWForm formData)
        {
            using var unityRequest = UnityWebRequest.Post(uri, formData);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(Uri uri, WWWForm formData)
        {
            using var unityRequest = UnityWebRequest.Post(uri, formData);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            using var unityRequest = UnityWebRequest.Post(uri, multipartFormSections, boundary);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(Uri uri, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            using var unityRequest = UnityWebRequest.Post(uri, multipartFormSections, boundary);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(string uri, Dictionary<string, string> formFields)
        {
            using var unityRequest = UnityWebRequest.Post(uri, formFields);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        public new static UnityWebRequest Post(Uri uri, Dictionary<string, string> formFields)
        {
            using var unityRequest = UnityWebRequest.Post(uri, formFields);
            DontDisposeHandlers(unityRequest);
            var sentryRequest = new SentryUnityWebRequest(uri, "POST");
            DuplicateHandlers(sentryRequest, unityRequest);
            return sentryRequest;
        }

        private static void DuplicateHandlers(SentryUnityWebRequest sentryRequest, UnityWebRequest unityRequest)
        {
            sentryRequest.uploadHandler = unityRequest.uploadHandler;
            sentryRequest.downloadHandler = unityRequest.downloadHandler;
        }

        private static void DontDisposeHandlers(UnityWebRequest request)
        {
            request.disposeDownloadHandlerOnDispose = false;
            request.disposeUploadHandlerOnDispose = false;
        }
    }
}
