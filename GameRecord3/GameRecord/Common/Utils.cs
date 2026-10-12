using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;

namespace GameRecord
{
    public static class Utils
    {
        public static string GetClientIp()
        {
            var request = HttpContext.Current.Request;
            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ip))
            {
                string[] ipList = ip.Split(',');
                ip = ipList[0].Trim();
                if (IsValidIp(ip))
                {
                    return ip;
                }
            }

            if (string.IsNullOrEmpty(ip)) // Check for localhost IPv6 loopback
            {
                ip = Dns.GetHostEntry(Dns.GetHostName())
                        .AddressList
                        .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                        .ToString();
                if (IsValidIp(ip))
                {
                    return ip;
                }
            }

            ip = request.ServerVariables["REMOTE_ADDR"];
            return IsValidIp(ip) ? ip : "1.1.1.1";
        }

        private static bool IsValidIp(string ip)
        {
            if (string.IsNullOrEmpty(ip) || string.Equals(ip, "::1"))
            {
                return false;
            }

            return System.Net.IPAddress.TryParse(ip, out _);
        }

        public static string SubStringTrackerId(string trackerId)
        {
            if (string.IsNullOrEmpty(trackerId))
            {
                throw new ArgumentException("Tracker ID cannot be null or empty.", nameof(trackerId));
            }

            return trackerId.Length > 16 ? trackerId.Substring(0, 16) : trackerId;
        }
    }
}