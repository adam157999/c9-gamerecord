using System;
using System.Net;
using System.Net.Sockets;
using System.Web.Mvc;

public class AzureNetworkController : Controller
{
    public ActionResult Index()
    {
        return Content("Outbound IP: " + GetOutboundIPAddress());
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
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}