using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Mvc;

public class NetworkController : Controller
{
    public async Task<ActionResult> Index()
    {
        var model = new AzureNetworkInfo
        {
            OutboundLocalIP = GetOutboundIPAddress(),
            PublicIP = await GetPublicIP(),
            Hostname = Environment.MachineName,
            InstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
            Region = Environment.GetEnvironmentVariable("REGION_NAME"),
            OutboundIPPool = Environment.GetEnvironmentVariable("WEBSITE_OUTBOUND_IP_ADDRESSES"),
            PossibleOutboundIPs = Environment.GetEnvironmentVariable("WEBSITE_POSSIBLE_OUTBOUND_IP_ADDRESSES"),
            SiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"),
            ContainerName = Environment.GetEnvironmentVariable("CONTAINER_NAME"),
            OSVersion = Environment.OSVersion.ToString()
        };

        return View(model);
    }

    private string GetOutboundIPAddress()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);
                var endpoint = socket.LocalEndPoint as IPEndPoint;
                return endpoint?.Address.ToString();
            }
        }
        catch
        {
            return "Unable to determine";
        }
    }

    private async Task<string> GetPublicIP()
    {
        using (var client = new HttpClient())
        {
            return await client.GetStringAsync("https://api.ipify.org");
        }
    }
}

public class AzureNetworkInfo
{
    public string OutboundLocalIP { get; set; }
    public string PublicIP { get; set; }
    public string Hostname { get; set; }
    public string InstanceId { get; set; }
    public string Region { get; set; }
    public string OutboundIPPool { get; set; }
    public string PossibleOutboundIPs { get; set; }
    public string SiteName { get; set; }
    public string ContainerName { get; set; }
    public string OSVersion { get; set; }
}