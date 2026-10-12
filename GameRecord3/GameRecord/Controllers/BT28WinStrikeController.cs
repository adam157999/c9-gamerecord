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
using System.Data.Entity.Validation;
using RestSharp;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GameRecord.Controllers
{
	public class BT28WinStrikeController : Controller
	{
		private string P_ApiUrl = "http://api.28win.com";
		private string apiUser = "bingo666";
		private string apiPass = "bingo666@win2020";
		private string user = "bingo666";
		private string pass = "778899";
		//private string prefix = "4d2";
		Common _common = new Common();

		public class WinNumberData
		{
			public string betID { get; set; }
			public string dateTime { get; set; }
			public string drawType { get; set; }
			public string drawDate { get; set; }
			public string betNumber { get; set; }
			public string strikePosition { get; set; }
			public string bet { get; set; }
			public string strike { get; set; }
		}

		public class AddScoreResultVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
		}

		//###################################################################################
		public bool GetPlayerCredit(string loginID, ref decimal credit, ref string errorDesc)
		{
			try
			{
				string method = "/getProfile.aspx";
				string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&loginID={4}", apiUser, apiPass, user, pass, loginID);

				string apiurl = P_ApiUrl + method + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				httpWebRequest.ContentType = "text/xml";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);

					string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
					errorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

					if (rErrorCode == "0")
					{
						credit = Convert.ToDecimal(xmlDoc.SelectSingleNode("response/balance").InnerText);
						return true;
					}
					else
					{
						credit = 0;
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/GetPlayerCredit" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				credit = 0;
				return false;
			}
		}

		public string GetPlayerPassword(string loginID)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
				{
					con.Open();
					using (SqlDataAdapter adapter = new SqlDataAdapter("select * from PLAYER_REC where KEY1 = '" + loginID + "'", con))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);
						if (table.Rows.Count == 1)
						{
							DataRow row = table.Rows[0];
							return row["STR1"].ToString();
						}
						else
						{
							return "";
						}
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/GetPlayerPassword" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				return "";
			}
		}

		public bool WithdrawFrom4DWallet(string loginID, string amount, ref string errorDesc)
		{
			try
			{
				string method = "/withdraw.aspx";
				string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&loginID={4}&amount={5}", apiUser, apiPass, user, pass, loginID, amount);

				string apiurl = P_ApiUrl + method + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				httpWebRequest.ContentType = "text/xml";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);

					string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
					errorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

					if (rErrorCode == "0")
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/WithdrawFrom4DWallet" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				return false;
			}
		}

		public bool TopupTo4DWallet(string loginID, string amount)
		{
			try
			{
				string method = "/deposit.aspx";
				string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&loginID={4}&amount={5}", apiUser, apiPass, user, pass, loginID, amount);

				string apiurl = P_ApiUrl + method + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				httpWebRequest.ContentType = "text/xml";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);

					string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
					string rErrorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

					if (rErrorCode == "0")
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/TopupTo4DWallet" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				return false;
			}
		}

		public void UpdateBetRecord(string betID, string remark = "", decimal? credit = null)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var query = db.BT28WinGameStrike.Where(s => s.BetID == betID && (s.IsProcess == false || s.IsProcess == null)).FirstOrDefault();
					if (query != null)
					{
						switch (remark)
						{
							case "Success":
								query.IsTransferred = true;
								query.IsProcess = true;
								query.Remark = "Success";
								query.TransferredDate = DateTime.Now;
								db.SaveChanges();
								break;
							case "Insufficient Credit":
								query.IsTransferred = false;
								query.IsProcess = true;
								query.Remark = "Insufficient Credit: " + Convert.ToString(credit);
								db.SaveChanges();
								break;
							default:
								query.IsTransferred = false;
								query.IsProcess = true;
								query.Remark = remark;
								db.SaveChanges();
								break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/UpdateBetRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
			}
		}

		public bool TransferToBTWallet(string loginID, string strike, ref string errorDesc)
		{
			try
			{
				var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=AddScore");
				client.Timeout = -1;
				var request = new RestRequest(Method.POST);
				request.AddHeader("Content-Type", "text/xml");
				request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
								+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
								+ "<soap:Body>\r\n    "
								+ "<AddScore xmlns=\"http://tempuri.org/\">\r\n      "
								+ "<CompanyID>BingoTown</CompanyID>\r\n      "
								+ "<Account>" + loginID.Substring(11) + "</Account>\r\n      "
								+ "<Password>" + GetPlayerPassword(loginID.Substring(11)) + "</Password>\r\n      "
								+ "<Value>" + Convert.ToInt32((Convert.ToDecimal(strike) * 100)).ToString() + "</Value>\r\n    "
								+ "</AddScore>\r\n  "
								+ "</soap:Body>\r\n"
								+ "</soap:Envelope>", ParameterType.RequestBody);
				IRestResponse response = client.Execute(request);

				XmlDocument xmlDoc1 = new XmlDocument();
				xmlDoc1.LoadXml(response.Content);
				string sInnerText = xmlDoc1.InnerText;
				var result = JsonConvert.DeserializeObject<AddScoreResultVM>(sInnerText);

				errorDesc = result.Message;

				if (result.Code == 1 && result.Message == "Success")
					return true;
				else
					return false;
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/TransferToBTWallet" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				return false;
			}
		}

		//public bool abc()
		//{
		//	try
		//	{
		//		XmlDocument xmlDoc = new XmlDocument();
		//		xmlDoc.LoadXml("<response><errorCode>0</errorCode><desc/><betData><betID>7086578914</betID><loginID>4d20165913906</loginID><dateTime>2020-07-25 21:24:26</dateTime><drawType>1</drawType><drawDate>2020-07-26</drawDate><betNumber>7654</betNumber><strikePosition>1</strikePosition><bet>1.0000</bet><strike>2,625.00</strike></betData><betData><betID>7086616498</betID><loginID>4d20165913906</loginID><dateTime>2020-07-25 21:37:37</dateTime><drawType>8</drawType><drawDate>2020-07-26</drawDate><betNumber>3448</betNumber><strikePosition>2</strikePosition><bet>0.0833</bet><strike>1,155.00</strike></betData></response>");
		//		string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
		//		string rErrorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

		//		if (rErrorCode == "0")
		//		{
		//			XmlNodeList xnList = xmlDoc.SelectNodes("response/betData");
		//			using (BingoConn db = new BingoConn())
		//			{
		//				foreach (XmlNode xn in xnList)
		//				{
		//					var betID = xn["betID"].InnerText.ToString();
		//					var query = db.BT28WinGameStrike.Where(s => s.BetID == betID && (s.IsProcess == false || s.IsProcess == null)).FirstOrDefault();
		//					if (query != null)
		//					{
		//						decimal credit = 0;
		//						string errorDesc = "";

		//						if (GetPlayerCredit(xn["loginID"].InnerText, ref credit, ref errorDesc))
		//						{
		//							if (credit >= Convert.ToDecimal(xn["strike"].InnerText))
		//							{
		//								//withdraw from 4d wallet first
		//								if (WithdrawFrom4DWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString(), ref errorDesc))
		//								{
		//									//then only transfer to bt wallet
		//									if (TransferToBTWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString(), ref errorDesc))
		//									{
		//										UpdateBetRecord(xn["betID"].InnerText, errorDesc);
		//									}
		//									//if transfer to bt wallet failed, topup back the 4d wallet
		//									else
		//									{
		//										UpdateBetRecord(xn["betID"].InnerText, errorDesc);
		//										TopupTo4DWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString());
		//									}
		//								}
		//								else
		//								{
		//									UpdateBetRecord(xn["betID"].InnerText, errorDesc);
		//								}
		//							}
		//							else
		//							{
		//								UpdateBetRecord(xn["betID"].InnerText, "Insufficient Credit", credit);
		//							}
		//						}
		//						else
		//						{
		//							UpdateBetRecord(xn["betID"].InnerText, errorDesc);
		//						}
		//					}
		//					else
		//					{
		//						_common.LogError("GetBetInBT28WinGame" + " " + DateTime.Now.ToString() + "  " + "Invalid Bet ID: " + xn["betID"].InnerText, "BT28WinStrike");
		//					}
		//				}
		//			}
		//		}
		//		else
		//		{
		//			_common.LogError("GetWinningNumber" + " " + DateTime.Now.ToString() + "  " + rErrorDesc, "BT28WinStrike");
		//		}

		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		_common.LogError("TransferCredit" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
		//		return false;
		//	}
		//}

		public bool TransferCredit()
		{
			try
			{
				string today = DateTime.Now.ToString("yyyy-MM-dd");
				string method = "/winNumber.aspx";
				string param = string.Format("?apiUser={0}&apiPass={1}&user={2}&pass={3}&dateFrom={4}&dateTo={5}", apiUser, apiPass, user, pass, today, today);

				string apiurl = P_ApiUrl + method + param;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
				httpWebRequest.ContentType = "text/xml";
				httpWebRequest.Method = "GET";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(responseText);
					string rErrorCode = xmlDoc.SelectSingleNode("response/errorCode").InnerText;
					string rErrorDesc = xmlDoc.SelectSingleNode("response/desc").InnerText;

					if (rErrorCode == "0")
					{
						XmlNodeList xnList = xmlDoc.SelectNodes("response/betData");
						using (BingoConn db = new BingoConn())
						{
							foreach (XmlNode xn in xnList)
							{
								var betID = xn["betID"].InnerText.ToString();
								var query = db.BT28WinGameStrike.Where(s => s.BetID == betID && (s.IsProcess == false || s.IsProcess == null)).FirstOrDefault();
								if (query != null)
								{
									decimal credit = 0;
									string errorDesc = "";

									if (GetPlayerCredit(xn["loginID"].InnerText, ref credit, ref errorDesc))
									{
										if (credit >= Convert.ToDecimal(xn["strike"].InnerText))
										{
											//withdraw from 4d wallet first
											if (WithdrawFrom4DWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString(), ref errorDesc))
											{
												//then only transfer to bt wallet
												if (TransferToBTWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString(), ref errorDesc))
												{
													UpdateBetRecord(xn["betID"].InnerText, errorDesc);
												}
												//if transfer to bt wallet failed, topup back the 4d wallet
												else
												{
													UpdateBetRecord(xn["betID"].InnerText, errorDesc);
													TopupTo4DWallet(xn["loginID"].InnerText.ToString(), xn["strike"].InnerText.ToString());
												}
											}
											else
											{
												UpdateBetRecord(xn["betID"].InnerText, errorDesc);
											}
										}
										else
										{
											UpdateBetRecord(xn["betID"].InnerText, "Insufficient Credit", credit);
										}
									}
									else
									{
										UpdateBetRecord(xn["betID"].InnerText, errorDesc);
									}
								}
								else
								{
									_common.LogError("BT28WinStrike/GetBetInBT28WinGame" + " " + DateTime.Now.ToString() + "  " + "Invalid Bet ID: " + xn["betID"].InnerText, "BT28WinStrike");
								}
							}
						}
					}
					else
					{
						_common.LogError("BT28WinStrike/GetWinningNumber" + " " + DateTime.Now.ToString() + "  " + rErrorDesc, "BT28WinStrike");
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("BT28WinStrike/TransferCredit" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
				return false;
			}
		}

		// GET: BT28WinStrike
		public void BT28WinStrikeJob()
		{
			try
			{
				bool isOK = TransferCredit();
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "BT28WinStrike");
			}
		}
	}
}