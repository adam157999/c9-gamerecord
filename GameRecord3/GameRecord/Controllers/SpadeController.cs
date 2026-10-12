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
using System.Globalization;

namespace GameRecord.Controllers
{
	public class SpadeController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SpadeGaming";
		//private string P_ApiUrl = "https://api-egame-staging.sgplay.net/api/";
		private string P_ApiUrl = "https://merchantapi.hugedolphin.com/api/";
		private string P_MerchantCode = "ZCH059";
		private string P_DataType = "JSON";
		private string P_DTFormat = "yyyyMMddTHHmmss";
		//private string P_Currency = "MYR";
		//private int P_TimeoutSec = 60000;
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
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");

			try
			{
				string sMainID = Guid.NewGuid().ToString();
				string method = "getBetHistory";
				string webAddr = P_ApiUrl;
				string serialNo = Guid.NewGuid().ToString();

				dynamic request = new ExpandoObject();
				request.beginDate = startTime;
				request.endDate = endTime;
				request.pageIndex = 1;
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
						SpadeGameMain spadeGameMain = new SpadeGameMain()
						{
							Id = sMainID,
							Total = Convert.ToInt32(result.resultCount),
							Code = result.code,
							Msg = result.msg,
							StartTime = DateTime.ParseExact(startTime, P_DTFormat, CultureInfo.InvariantCulture), //Convert.ToDateTime(startTime),
							EndTime = DateTime.ParseExact(endTime, P_DTFormat, CultureInfo.InvariantCulture), //Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SpadeGameMains.Add(spadeGameMain);
						db.SaveChanges();

						if (result.code == 0)
						{
							if (Convert.ToInt32(result.resultCount) > 0)
							{
								foreach (var obj in result.list)
								{
									string ticketId = Convert.ToString(obj.ticketId);

									try
									{
										SpadeGame spadeGame = new SpadeGame()
										{
											fId = sMainID,
											ticketId = obj.ticketId,
											acctId = obj.acctId,
											ticketTime = obj.ticketTime != null ? DateTime.ParseExact(Convert.ToString(obj.ticketTime), P_DTFormat, CultureInfo.InvariantCulture) : null,
											categoryId = obj.categoryId,
											gameCode = obj.gameCode,
											currency = obj.currency,
											betAmount = obj.betAmount != null ? Convert.ToDecimal(obj.betAmount) : null,
											result = obj.result,
											winLoss = obj.winLoss != null ? Convert.ToDecimal(obj.winLoss) : null,
											jackpotAmount = obj.jackpotAmount != null ? Convert.ToDecimal(obj.jackpotAmount) : null,
											betIp = obj.betIp,
											luckyDrawId = obj.luckyDrawId,
											completed = obj.completed != null ? Convert.ToBoolean(obj.completed) : null,
											roundId = obj.roundId,
											sequence = obj.sequence,
											channel = obj.channel,
											balance = obj.balance != null ? Convert.ToDecimal(obj.balance) : null,
											jpWin = obj.jpWin != null ? Convert.ToDecimal(obj.jpWin) : null,
											referenceId = obj.referenceId
										};

										var record = db.SpadeGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

										if (record == null)
										{
											spadeGame.CreatedDate = DateTime.Now;
										}
										else
										{
											spadeGame.CreatedDate = record.CreatedDate;
											spadeGame.ModifiedDate = DateTime.Now;
										}
										db.SpadeGames.AddOrUpdate(spadeGame);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + ticketId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Spade");
									}
								}
								db.SaveChanges();

								if (Convert.ToInt16(result.pageCount) > 1)
								{
									for (int x = 2; x <= Convert.ToInt16(result.pageCount); x++)
									{
										bool IsOK = GetGameRecordExtend(x, startTime, endTime, sMainID);
									}
								}
							}
						}

						if (result.code == 0)
						{
							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
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

		public bool GetGameRecordExtend(int pageIndex, string startTime, string endTime, string sMainID)
		{
			try
			{
				string method = "getBetHistory";
				string webAddr = P_ApiUrl;
				string serialNo = Guid.NewGuid().ToString();

				dynamic request = new ExpandoObject();
				request.beginDate = startTime;
				request.endDate = endTime;
				request.pageIndex = pageIndex;
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
							if (Convert.ToInt32(result.resultCount) > 0)
							{
								foreach (var obj in result.list)
								{
									string ticketId = Convert.ToString(obj.ticketId);

									try
									{
										SpadeGame spadeGame = new SpadeGame()
										{
											fId = sMainID,
											ticketId = obj.ticketId,
											acctId = obj.acctId,
											ticketTime = obj.ticketTime != null ? DateTime.ParseExact(Convert.ToString(obj.ticketTime), P_DTFormat, CultureInfo.InvariantCulture) : null,
											categoryId = obj.categoryId,
											gameCode = obj.gameCode,
											currency = obj.currency,
											betAmount = obj.betAmount != null ? Convert.ToDecimal(obj.betAmount) : null,
											result = obj.result,
											winLoss = obj.winLoss != null ? Convert.ToDecimal(obj.winLoss) : null,
											jackpotAmount = obj.jackpotAmount != null ? Convert.ToDecimal(obj.jackpotAmount) : null,
											betIp = obj.betIp,
											luckyDrawId = obj.luckyDrawId,
											completed = obj.completed != null ? Convert.ToBoolean(obj.completed) : null,
											roundId = obj.roundId,
											sequence = obj.sequence,
											channel = obj.channel,
											balance = obj.balance != null ? Convert.ToDecimal(obj.balance) : null,
											jpWin = obj.jpWin != null ? Convert.ToDecimal(obj.jpWin) : null,
											referenceId = obj.referenceId
										};

										var record = db.SpadeGames.Where(x => x.ticketId == ticketId).FirstOrDefault();

										if (record == null)
										{
											spadeGame.CreatedDate = DateTime.Now;
										}
										else
										{
											spadeGame.CreatedDate = record.CreatedDate;
											spadeGame.ModifiedDate = DateTime.Now;
										}
										db.SpadeGames.AddOrUpdate(spadeGame);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + ticketId + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Spade");
									}
								}
								db.SaveChanges();
							}
						}
					}
				}

				return true;
			}
			catch(Exception ex)
			{
				_common.LogError("Spade/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
				throw;
			}
		}

		public string ComputeHash(byte[] body)
		{
			HashAlgorithm DigestProvider = new MD5CryptoServiceProvider();
			byte[] hashBytes = DigestProvider.ComputeHash(body);
			string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
			return hash;
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				//DateTime dtEnd = Convert.ToDateTime(endTime);
				DateTime dtEnd = DateTime.ParseExact(endTime, P_DTFormat, CultureInfo.InvariantCulture);

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.LastRunTillDate = dtEnd;

						bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Spade/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
				return false;
			}
		}

		#endregion

		//###################################################################################
		#region Function 

		public string GetJobTime(string action)
		{
			try
			{
				string sTime = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString(P_DTFormat);
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString(P_DTFormat);
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Spade/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Spade");
				return "Error:" + ex.Message.ToString();
			}
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