using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
    public class SendFlyController : Controller
    {

        private const string ApiKey = "zMPYJtgEEWVmmilSuZVFwA==";
        private const string ApiSecret = "0a1fe8bae16f41fabcb747cdbef0d222";
        private const string ApiUrl = "https://api.i51sms.com/outauth/verifCodeSend";


        public class SmsRequest
        {
            [Required(ErrorMessage = "mobileno is required")]
            public string mobileno { get; set; }

            [Required(ErrorMessage = "content is required")]
            public string content { get; set; }

            [MaxLength(11, ErrorMessage = "senderid must not exceed 11 characters")]
            public string senderid { get; set; }
        }

        public JsonResultContent JsonResultRes { get; set; }

        [System.Web.Mvc.HttpPost]
        public JsonResult SmsSend([FromBody] SmsRequest request)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors
                }, JsonRequestBehavior.AllowGet);
            }


            if (request == null)
                return Json(new { success = false, errormessage = "Request body is missing or invalid." });

            if (string.IsNullOrWhiteSpace(request.mobileno) || !System.Text.RegularExpressions.Regex.IsMatch(request.mobileno, @"^\d{10,15}$"))
                return Json(new { success = false, errormessage = "Invalid mobile number format." });

            if (string.IsNullOrWhiteSpace(request.content))
                return Json(new { success = false, errormessage = "Content must not be empty." });

            if (!string.IsNullOrEmpty(request.senderid) && request.senderid.Length > 11)
                return Json(new { success = false, errormessage = "Sender ID must not exceed 11 characters." });




            var apiCall = Task.Run(() => SendSmsAsync(request.mobileno, request.content, request.senderid)).Result;

            if (apiCall)
            {
                return Json(new { success = true, errormessage = JsonResultRes.msg });
            }
            else
            {
                return Json(new { success = false , errormessage = JsonResultRes .msg});
            }

            
        }


        public async Task<bool> SendSmsAsync(string mobileno, string content, string senderId = null)
        {
            string timestamp = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
            string sign = GenerateMD5(ApiKey + timestamp + ApiSecret);

            var payload = new
            {
                apikey = ApiKey,
                timestamp = timestamp,
                sign = sign,
                mobile = mobileno,
                content = content,
                senderid = senderId
            };

            HttpClient client = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(ApiUrl, contentData);

            string result = await response.Content.ReadAsStringAsync();

            JsonResultRes = JsonSerializer.Deserialize<JsonResultContent>(result);

            if (JsonResultRes.status == "000" && JsonResultRes.code == "")
            {
                return true;
            }
            else
            {
                return false;

            }

        }

        private string GenerateMD5(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }


    }

    public class Data
    {
        public int validnumber { get; set; }
        public int jfts { get; set; }
        public long taskid { get; set; }
        public List<object> invalidNumbers { get; set; }
    }

    public class JsonResultContent
    {
        public string status { get; set; }
        public string msg { get; set; }
        public string code { get; set; }
        public Data data { get; set; }
    }

}