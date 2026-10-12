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

namespace GameRecord.Controllers
{
	public class PTJobController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PT";
		private string P_ApiUrl = "https://kioskpublicapi.luckydragon88.com/";
		private string P_EntityKey = "ee1976197b52c2fa60e05e1d489810c3de3dc6ab184508f97cbabe54b21dc39838dd0ce3f8f1377f9fba3ac0441d1988a7e51d2f43fcfd192376b476f154635d";
		private string P_CertPass = "vrinkiv4HfXj7flP";
		private string P_CertPath = System.Web.HttpContext.Current.Server.MapPath(@"\Keys\MYRWFF88.p12");
		//private string P_CertPath = System.Web.HttpContext.Current.Server.MapPath(@"\Keys\CNY_UAT_FB88.p12");
		//private string P_CertPath = "C:\\inetpub\\wwwroot\\GameRecord Publish\\Keys\\CNY_UAT_FB88.p12";
		Common _common = new Common();
		private PTConnector api = new PTConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//###################################################################################
		#region Declare 

		public class GameResponse
		{
			public Pagination pagination { get; set; }
			public List<Result> result { get; set; }
			public string error { get; set; }
			public int errorcode { get; set; }
		}

		public class Pagination
		{
			public int currentPage { get; set; }
			public int totalPages { get; set; }
			public int itemsPerPage { get; set; }
			public int totalCount { get; set; }
		}

		public class Result
		{
			public string PLAYERNAME { get; set; }
			public string WINDOWCODE { get; set; }
			public string GAMEID { get; set; }
			public string GAMECODE { get; set; }
			public string GAMETYPE { get; set; }
			public string GAMENAME { get; set; }
			public string SESSIONID { get; set; }
			public decimal BET { get; set; }
			public decimal WIN { get; set; }
			public decimal PROGRESSIVEBET { get; set; }
			public decimal PROGRESSIVEWIN { get; set; }
			public decimal BALANCE { get; set; }
			public decimal CURRENTBET { get; set; }
			public DateTime? GAMEDATE { get; set; }
			public string LIVENETWORK { get; set; }
			public string RNUM { get; set; }
		}

		#endregion

		private static bool CertificateValidationCallBack(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			// If the certificate is a valid, signed certificate, return true.
			if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
			{
				return true;
			}

			// If there are errors in the certificate chain, look at each error to determine the cause.
			if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
			{
				if (chain != null && chain.ChainStatus != null)
				{
					foreach (X509ChainStatus status in chain.ChainStatus)
					{
						if ((certificate.Subject == certificate.Issuer) && (status.Status == X509ChainStatusFlags.UntrustedRoot))
						{
							// Self-signed certificates with an untrusted root are valid. 
							continue;
						}
						else
						{
							if (status.Status != X509ChainStatusFlags.NoError)
							{
								// If there are any other errors in the certificate chain, the certificate is invalid,
								// so the method returns false.
								return false;
							}
						}
					}
				}

				return true;
			}
			else
			{
				// In all other cases, return false.
				return false;
			}
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PT/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 15, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 0, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("PT/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			int currentPage = 1;
			int pageSize = 5000;
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 120;

			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
					{
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));
					}

					int errorCode = 0;
					string errorMessage = "";
					PlayTech.GameTransactionsResponse resp = api.GameTransactions(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), ref errorCode, ref errorMessage, currentPage, pageSize);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						PTGameMain ptGameMain = new PTGameMain()
						{
							Id = sMainID,
							Total = 0,
							ErrorCode = errorCode,
							Error = errorMessage,
							StartTime = Convert.ToDateTime(mStartTime),
							EndTime = Convert.ToDateTime(mEndTime),
							CreatedDate = DateTime.Now
						};
						db.PTGameMains.Add(ptGameMain);
						db.SaveChanges();

						if (resp.data != null)
						{
							if (resp.data.data != null)
							{
								foreach (var obj in resp.data.data)
								{
									string refNo = Convert.ToString(obj.game_server_reference_1);

									try
									{
										if (db.PTGameTrans.Where(x => x.game_server_reference_1 == refNo).FirstOrDefault() == null)
										{
											PTGameTran ptGame = new PTGameTran()
											{
												fId = sMainID,
												reference_no = obj.reference_no,
												entity_name = obj.entity_name,
												kiosk_name = obj.kiosk_name,
												game_server = obj.game_server,
												gamzo_player_name = obj.gamzo_player_name,
												game_name = obj.game_name,
												game_shortcode = obj.game_shortcode,
												game_type = obj.game_type,
												currency = obj.currency,
												is_win = obj.is_win,
												bet = obj.bet,
												win = obj.win,
												progressive_bet = obj.progressive_bet,
												progressive_win = obj.progressive_win,
												exit_type = obj.exit_type,
												bonus_type = obj.bonus_type,
												game_server_reference_1 = obj.game_server_reference_1,
												game_server_session_id = obj.game_server_session_id,
												bet_timestamp = obj.bet_timestamp,
												bet_datetime = obj.bet_datetime,
												game_snapshot_token = obj.game_snapshot_token,
												game_snapshot = obj.game_snapshot,
												balance_after = obj.balance_after,
												hash = obj.hash,
												CreatedDate = DateTime.Now
											};
											db.PTGameTrans.Add(ptGame);
										}
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + refNo + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
									}
								}
								db.SaveChanges();
							}

							if (resp.data.pagination != null)
							{
								//record more than limit
								bool HasNextPage = resp.data.pagination.has_next_page;
								int Page = resp.data.pagination.page;

								while (HasNextPage == true)
								{
									Page++;
									GetGameRecordExtend(pageSize, mStartTime, mEndTime, sMainID, ref HasNextPage, ref Page);
								}
								bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
							}
						}
						else
						{
							bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PT/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
				return false;
			}
		}

		public bool GetGameRecordExtend(int pageSize, string startTime, string endTime, string sMainID, ref bool HasNextPage, ref int page)
		{
			int errorCode = 0;
			string errorMessage = "";
			PlayTech.GameTransactionsResponse resp = api.GameTransactions(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), ref errorCode, ref errorMessage, page, pageSize);

			using (BingoConn db = new BingoConn())
			{
				if (resp.data != null)
				{
					if (resp.data.pagination != null)
					{
						HasNextPage = resp.data.pagination.has_next_page;
						page = resp.data.pagination.page;
					}

					if (resp.data.data != null)
					{
						foreach (var obj in resp.data.data)
						{
							string refNo = Convert.ToString(obj.game_server_reference_1);

							try
							{
								if (db.PTGameTrans.Where(x => x.game_server_reference_1 == refNo).FirstOrDefault() == null)
								{
									PTGameTran ptGame = new PTGameTran()
									{
										fId = sMainID,
										reference_no = obj.reference_no,
										entity_name = obj.entity_name,
										kiosk_name = obj.kiosk_name,
										game_server = obj.game_server,
										gamzo_player_name = obj.gamzo_player_name,
										game_name = obj.game_name,
										game_shortcode = obj.game_shortcode,
										game_type = obj.game_type,
										currency = obj.currency,
										is_win = obj.is_win,
										bet = obj.bet,
										win = obj.win,
										progressive_bet = obj.progressive_bet,
										progressive_win = obj.progressive_win,
										exit_type = obj.exit_type,
										bonus_type = obj.bonus_type,
										game_server_reference_1 = obj.game_server_reference_1,
										game_server_session_id = obj.game_server_session_id,
										bet_timestamp = obj.bet_timestamp,
										bet_datetime = obj.bet_datetime,
										game_snapshot_token = obj.game_snapshot_token,
										game_snapshot = obj.game_snapshot,
										balance_after = obj.balance_after,
										hash = obj.hash,
										CreatedDate = DateTime.Now
									};
									db.PTGameTrans.Add(ptGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Record " + refNo + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
							}
						}
						db.SaveChanges();
					}
				}
			}
			//}
			return true;
		}

		//http://localhost:51148/PTJob/PTJob
		public void PTJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PT");
			}
		}
	}

	public class TrustAllCertificatePolicy : ICertificatePolicy
	{
		public TrustAllCertificatePolicy()
		{ }
		public bool CheckValidationResult(ServicePoint sp, System.Security.Cryptography.X509Certificates.X509Certificate cert, WebRequest req, int problem)
		{
			return true;
		}
	}
}