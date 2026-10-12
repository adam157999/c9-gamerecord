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
	public class SpadeGameListController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SpadeGaming";
		private string P_ApiUrl = "https://merchantapi.hugedolphin.com/api/";
		private string P_MerchantCode = "ZCH059";
		private string P_DataType = "JSON";
		private string P_DTFormat = "yyyyMMddTHHmmss";
		Common _common = new Common();

		public const string AcceptEncoding = "Accept-Encoding";
		public const string AcceptLanguage = "Accept-Language";
		public const string ContentEncoding = "Content-Encoding";
		public const string Api = "API";
		public const string DataType = "DataType";
		public const string Disgest = "Digest";
		public static readonly Encoding Charset = Encoding.UTF8;



		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			try
			{
				string sMainID = Guid.NewGuid().ToString();
				string method = "getGames";
				string webAddr = P_ApiUrl;
				string serialNo = Guid.NewGuid().ToString();

				dynamic request = new ExpandoObject();
				request.currency = "MYR";
				request.serialNo = serialNo;
				request.merchantCode = P_MerchantCode;

				string requestStr = JsonConvert.SerializeObject(request);
				byte[] body = Charset.GetBytes(requestStr);
				string hash = ComputeHash(body);

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentLength = body.Length;
				httpWebRequest.Headers.Add(Api, method);
				httpWebRequest.Headers.Add(DataType, P_DataType);
				httpWebRequest.Headers.Add(AcceptEncoding, "gzip");
				httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
				httpWebRequest.Headers.Add(Disgest, hash);

				using (Stream os = httpWebRequest.GetRequestStream())
				{
					os.Write(body, 0, body.Length);
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						if (result.code == 0)
						{
							foreach (var obj in result.games)
							{
								string gameCode = Convert.ToString(obj.gameCode);

								try
								{
									ADMGameSetup game = new ADMGameSetup()
									{
										GameProvider = "SPADEGAMING",
										GameID = obj.gameCode,
										GameNameEN = obj.gameName
									};

									db.ADMGameSetups.AddOrUpdate(game);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + gameCode + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Spade");
								}
							}
							db.SaveChanges();

						}
					}
				}


				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Spade/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
				return false;
			}
		}

		public string ComputeHash(byte[] body)
		{
			HashAlgorithm DigestProvider = new MD5CryptoServiceProvider();
			byte[] hashBytes = DigestProvider.ComputeHash(body);
			string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
			return hash;
		}

		#endregion


		// GET: VPower
		public void SpadeJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
			}
		}
	}
}