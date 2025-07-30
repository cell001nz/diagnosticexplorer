﻿using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Diagnostics.Service.Common.Hubs;

public class WebApiUtil
{

    private static async Task<HttpResponseMessage> SendRequest(string uri, HttpMethod method, object? arg = null)
    {
        using HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = true };
        using HttpClient httpClient = new HttpClient(handler);
        using HttpRequestMessage request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (arg != null)
        {
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg)));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return await httpClient.SendAsync(request);
    }
		

    public static async Task<string> Get(string url)
    {
        HttpResponseMessage response = await SendRequest(url, HttpMethod.Get);
        string content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
        {
            throw new ServiceException(response.StatusCode, GetErrorMessage(content));
        }

        return content;
    }

    public static async Task<T> Get<T>(string url)
    {
        HttpResponseMessage response = await SendRequest(url, HttpMethod.Get);

        string content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
            throw new ServiceException(response.StatusCode, GetErrorMessage(content));

        T result = JsonConvert.DeserializeObject<T>(content)!;
        return result;
    }

    public static async Task<string> Post(string url, object param)
    {
        HttpResponseMessage response = await SendRequest(url, HttpMethod.Post, param);
        string content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            throw new ServiceException(response.StatusCode, GetErrorMessage(content));

        return content;
    }

    public static async Task<T> Post<T>(string url, object param)
    {
        HttpResponseMessage response = await SendRequest(url, HttpMethod.Post, param);

        string content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
            throw new ServiceException(response.StatusCode, GetErrorMessage(content));

        T result = JsonConvert.DeserializeObject<T>(content)!;
        return result;
    }


    private static string GetErrorMessage(string content)
    {
        try
        {
            return JsonConvert.DeserializeObject<string>(content) ?? "Unknown Error";
        }
        catch
        {
            return content;
        }
    }
}

public class ServiceException : Exception
{
    public HttpStatusCode StatusCode { get; set; }
    public ServiceException(HttpStatusCode httpStatusCode, string message)
        : base(message)
    {
        StatusCode = httpStatusCode;
    }
}