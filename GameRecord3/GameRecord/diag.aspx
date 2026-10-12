<%@ Page Language="C#" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Sockets" %>
<%@ Import Namespace="System.Net.Http" %>

<script runat="server">

string GetOutboundIP()
{
    try
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
    }
    catch (Exception ex)
    {
        return ex.Message;
    }
}

string GetPublicIP()
{
    try
    {
        using (var client = new WebClient())
        {
            return client.DownloadString("https://api.ipify.org");
        }
    }
    catch (Exception ex)
    {
        return ex.Message;
    }
}

</script>

<html>
<head>
<title>Azure Network Diagnostic</title>
</head>

<body style="font-family:Arial">

<h2>Azure App Service Network Diagnostic</h2>

<table border="1" cellpadding="8">

<tr>
<td><b>Outbound Local IP</b></td>
<td><%= GetOutboundIP() %></td>
</tr>

<tr>
<td><b>Public Internet IP</b></td>
<td><%= GetPublicIP() %></td>
</tr>

<tr>
<td><b>Azure Outbound IP Pool</b></td>
<td><%= Environment.GetEnvironmentVariable("WEBSITE_OUTBOUND_IP_ADDRESSES") %></td>
</tr>

<tr>
<td><b>Possible Outbound IPs</b></td>
<td><%= Environment.GetEnvironmentVariable("WEBSITE_POSSIBLE_OUTBOUND_IP_ADDRESSES") %></td>
</tr>

<tr>
<td><b>Instance ID</b></td>
<td><%= Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") %></td>
</tr>

<tr>
<td><b>Region</b></td>
<td><%= Environment.GetEnvironmentVariable("REGION_NAME") %></td>
</tr>

<tr>
<td><b>Site Name</b></td>
<td><%= Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") %></td>
</tr>

<tr>
<td><b>Server Name</b></td>
<td><%= Environment.MachineName %></td>
</tr>

<tr>
<td><b>OS Version</b></td>
<td><%= Environment.OSVersion %></td>
</tr>

</table>

</body>
</html>
