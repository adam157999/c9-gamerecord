using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using GameRecord.Models;
using RestSharp;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class VPowerGameListController : Controller
	{
		//public static readonly object MyLock = new object();
		//private string P_GamePrefix = "VPower";
		//private string P_ApiUrl = "https://www.stargame88.xyz/open_api?";
		//private string P_SecretKey = "K85FYkLoJYjulainuZU1ghx3jd39";
		//private string P_AppId = "F00CNcapRVkdxtty4d4";

		public static readonly object MyLock = new object();
		private string P_GamePrefix = "VPower";
		private string P_ApiUrl = "https://www.vpower123.com/open_api?";
		private string P_SecretKey = "J00CZNfYVWpXgbjWnojufvn4d4";
		private string P_AppId = "G85FkVQXNZoulld39";
		private int P_TimeoutSec = 60000;
		Common _common = new Common();



		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			try
			{
				string action = "newgamelist";
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string rawParam = string.Format("Act={0}&Timestamp={1}&AppId={2}", action, time, P_AppId);
				string signature = hash_hmac(rawParam);
				string param = string.Format("Act={0}&Timestamp={1}&AppId={2}&Signature={3}", action, time, P_AppId, signature);
				string webAddr = P_ApiUrl + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						if (result.ErrorCode == 0)
						{
							if (result.GameList.Count > 0)
							{
								foreach (var obj in result.GameList)
								{
									string GameID = Convert.ToString(obj.GameID);

									try
									{
										ADMGameSetup game = new ADMGameSetup()
										{
											GameProvider = "VPOWER",
											ModuleId = obj.GameOCode,
											ModuleNameEN = obj.GameType,
											GameID = obj.GameID,
											GameNameCN = obj.GameName,
											GameNameEN = obj.GameName_EN
										};

										var record = db.ADMGameSetups.Where(x => x.GameID == GameID && x.GameProvider.Trim().ToUpper() == "VPOWER").FirstOrDefault();

										db.ADMGameSetups.AddOrUpdate(game);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + GameID + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "VPower");
									}
								}
								db.SaveChanges();
							}
						}
					}
				}


				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("VPower/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
				return false;
			}
		}

		public string hash_hmac(string signatureString)
		{
			using (var signature = new HMACSHA1(Encoding.UTF8.GetBytes(P_SecretKey)))
			{
				var bytes = Encoding.UTF8.GetBytes(signatureString);
				var hash = signature.ComputeHash(bytes);
				return HttpUtility.UrlEncode(Convert.ToBase64String(hash));
			}
		}

		public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}


		#endregion

		// GET: VPower
		public void VPowerJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "VPower");
			}
		}
	}
}