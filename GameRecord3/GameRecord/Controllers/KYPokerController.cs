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
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class KYPokerController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "KYPoker";
		private string P_FilePrefix = "KYPOKER";
		//private string P_ApiUrl = "https://ldwbky.ynszmkj.com/getRecordHandle";
		//private string P_Agent = "60602";
		//private string P_DESKey = "770E9B775F612C91";
		//private string P_MD5Key = "79B1850F4EF7E810";
		private string P_ApiUrl = "https://ld.ky0228.com/getRecordHandle";
		private string P_Agent = "60016";
		private string P_DESKey = "4676289F3B129798";
		private string P_MD5Key = "E6E36CE4CD26DDF8";
		private int P_Interval = 20000;
		Common _common = new Common();

		public string AesEncrypt(string str, string key, bool isEncodeUrl = true)
		{
			if (string.IsNullOrEmpty(str)) return null;
			Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

			System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
			{
				Key = Encoding.UTF8.GetBytes(key),
				Mode = System.Security.Cryptography.CipherMode.ECB,
				Padding = System.Security.Cryptography.PaddingMode.PKCS7
			};

			System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
			Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

			var base64Str = Convert.ToBase64String(resultArray, 0, resultArray.Length);

			return isEncodeUrl ? System.Web.HttpUtility.UrlEncode(base64Str) : base64Str;
		}

		public string MD5Encrypt(string text)
		{
			byte[] ba = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(text));
			string hex = BitConverter.ToString(ba).ToLower();
			hex = hex.Replace("-", "");

			return hex;
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

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
				_common.LogError("KYPoker/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

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
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("KYPoker/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 40;
			int P_TimeoutSec = 300000;

			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					System.Threading.Thread.Sleep(P_Interval);
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
					{
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));
					}

					long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					string rawParam = $"s=6&startTime={new DateTimeOffset(Convert.ToDateTime(mStartTime)).ToUnixTimeMilliseconds()}&endTime={new DateTimeOffset(Convert.ToDateTime(mEndTime)).ToUnixTimeMilliseconds()}";
					string param = AesEncrypt(rawParam, P_DESKey);
					string key = MD5Encrypt(P_Agent + time.ToString() + P_MD5Key);
					string fullParam = $"?agent={P_Agent}&timestamp={time}&param={param}&key={key}";
					string webAddr = P_ApiUrl + fullParam;

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
							string sMainID = Guid.NewGuid().ToString();
							KYPokerGameMain kyGameMain = new KYPokerGameMain()
							{
								Id = sMainID,
								Total = result.d != null ? result.d.count : 0,
								Code = result.d != null ? result.d.code : 0,
								StartTime = Convert.ToDateTime(mStartTime),
								EndTime = Convert.ToDateTime(mEndTime),
								CreatedDate = DateTime.Now
							};
							db.KYPokerGameMains.Add(kyGameMain);
							db.SaveChanges();

							if (result.d != null)
							{
								if (result.d.code == 0)
								{
									if (result.d.list != null)
									{
										for (int i = 0; i < Convert.ToInt32(result.d.count); i++)
										{
											string gameid = Convert.ToString(result.d.list.GameID[i]);
											try
											{
												KYPokerGame kyGame = new KYPokerGame()
												{
													fId = sMainID,
													GameID = result.d.list.GameID[i],
													Accounts = result.d.list.Accounts[i],
													ServerID = result.d.list.ServerID[i],
													KindID = result.d.list.KindID[i],
													TableID = result.d.list.TableID[i],
													ChairID = result.d.list.ChairID[i],
													UserCount = result.d.list.UserCount[i],
													CardValue = result.d.list.CardValue[i],
													CellScore = result.d.list.CellScore[i],
													AllBet = result.d.list.AllBet[i],
													Profit = result.d.list.Profit[i],
													Revenue = result.d.list.Revenue[i],
													GameStartTime = result.d.list.GameStartTime[i],
													GameEndTime = result.d.list.GameEndTime[i],
													ChannelID = result.d.list.ChannelID[i],
													LineCode = result.d.list.LineCode[i]
												};

												var record = db.KYPokerGames.Where(x => x.GameID == gameid).FirstOrDefault();
												if (record == null)
												{
													kyGame.CreatedDate = DateTime.Now;
												}
												else
												{
													kyGame.CreatedDate = record.CreatedDate;
													kyGame.ModifiedDate = DateTime.Now;
												}
												db.KYPokerGames.AddOrUpdate(kyGame);
											}
											catch (Exception ex)
											{
												_common.LogError("Record " + gameid + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
											}
										}
									}
									db.SaveChanges();

								}

								if (result.d.code == 0 || result.d.code == 16)
								{
									bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
								}
							}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("KYPoker/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public void KYPokerJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}
	}
}