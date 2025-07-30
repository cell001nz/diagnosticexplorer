// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var httpClient = new HttpClient();


var nr = await httpClient.GetFromJsonAsync<NegotiateResponse>("http://localhost:4280/api/Negotiate");

try
{
    Console.WriteLine($"Negotiate Url: {nr.Url}");
    Console.WriteLine($"Negotiate AccessToken: {nr.AccessToken}");
    
    var hub = new HubConnectionBuilder().WithUrl(nr.Url, options =>
    {
        options.UseDefaultCredentials = true;
        options.AccessTokenProvider = () => Task.FromResult(nr.AccessToken)!;
    }).Build();

    hub.On("newMessage", (string msg) => Console.WriteLine($"Received message => {msg}"));

    hub.Closed += (error) =>
    {
        Console.WriteLine($"Closed: {error}");
        return Task.CompletedTask;
    };
    
    hub.Reconnecting += (error) =>
    {
        Console.WriteLine($"Reconnecting to SignalR hub... {error}");
        return Task.CompletedTask;
    };
    
    hub.Reconnected += (error) =>
    {
        Console.WriteLine($"Reconnected to SignalR hub...{error}");
        return Task.CompletedTask;
    };

    await hub.StartAsync();
    Console.WriteLine("Connected to SignalR hub!");

    while (true)
    {
        Console.WriteLine($"Sending1");
        await hub.InvokeAsync("broadcast1", "Calling broadcast1");
        Console.WriteLine($"Sent broadcast1");

        Console.WriteLine($"Sending2");
        await httpClient.GetAsync("http://localhost:4280/api/Broadcast2?message=calling-broadcast-2");
        // await hub.SendCoreAsync("broadcast1", ["Hello from client!"]);
        Console.WriteLine("Sent broadcast2");

        if (Console.ReadLine() == "exit") break;
    }
    


    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to SignalR hub: {ex.Message}");
}

public class NegotiateResponse
{
    public string Url { get; set; }
    public string AccessToken { get; set; }
}