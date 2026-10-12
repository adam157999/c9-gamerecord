using GameRecord.Controllers.SeamlessWallet.Model;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web.Http;
using System.Xml;
using System.Xml.Serialization;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers.SeamlessWallet
{

	public class PragmaticSeamlessController : ApiController
	{
		private string p_ErrorMessage = "";
		private static string gameCode = "PPSLOT";
		private static string filePrefix = "PPSLOT";
		private static string env = GameEnvironment.PROD.ToString();
		private static string secretKey = string.Empty;
		private static bool seamlessFlag = Convert.ToBoolean(ConfigurationManager.AppSettings["SeamlessRecordOnPayout"]);

		public PragmaticSeamlessController()
		{
			secretKey = GetPassKeyValue();
		}

		#region Private Function
		public static string GetMD5Signature(string param)
		{
			return BuildMD5(param + secretKey);
		}

		public static string BuildMD5(string inString)
		{
			byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hashed.Length; i++)
			{
				sb.Append(hashed[i].ToString("x2"));
			}
			return sb.ToString();
		}

		private string ConvertClassToXMLObject<T>(object TargetClass)
		{
			string jsonString = System.Text.Json.JsonSerializer.Serialize(TargetClass);
			XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonString);
			using (var stringWriter = new StringWriter())
			using (var xmlTextWriter = XmlWriter.Create(stringWriter))
			{
				doc.WriteTo(xmlTextWriter);
				xmlTextWriter.Flush();
				return stringWriter.GetStringBuilder().ToString();
			}
		}

		private string GetPassKeyValue()
		{
			var apiConfig = CatchData.GetApiKeys(gameCode, env);

			if (apiConfig != null)
			{
				JObject jObj = JObject.Parse(apiConfig.ApiConfig);
				return jObj["PragmaticSecretKey"].ToString();
			}

			return string.Empty;
		}
		
		private bool VerifyMD5Signature(BaseRequest request, string hash)
		{
			bool flag = true;
			string hashString = GetMD5Signature(request.GetParameters());
			if (hashString != hash)
			{
				throw new Exception("Hash Value Error");
			}

			return flag;

		}
		#endregion

		[HttpPost]
		public HttpResponseMessage Balance(PragmaticBalanceRequest rqdata)
		{
			Common _common = new Common();
			PragmaticBalanceResponse response = new PragmaticBalanceResponse();
			try
			{
				var param = "providerId=" + rqdata.providerId + "&userId=" + rqdata.userId;
				var compareParam = GetMD5Signature(param);

				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
							x => x.Game == gameCode
					&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					response.currency = foundRecord.Currency;
					response.cash = Math.Round(foundRecord.Cash + foundRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:GetBalance:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("Balance", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage Authenticate(PragmaticVerifySessionRequest rqdata)
		{
			Common _common = new Common();
			//_common.LogError($"VerifySession:START " + JsonConvert.SerializeObject(rqdata), filePrefix);

			PragmaticVerifySessionResponse response = new PragmaticVerifySessionResponse();
			try
			{
				var param = "providerId=" + rqdata.providerId + "&token=" + rqdata.token;
				var compareParam = GetMD5Signature(param);

				// --- TEMP DIAGNOSTIC LOGGING ---
				_common.LogError(
					$"VerifySession:DEBUG gameCode={gameCode} providerId={rqdata.providerId} " +
					$"token={rqdata.token} incomingHash={rqdata.hash} computedHash={compareParam} " +
					$"paramString=[{param}]",
					filePrefix);
				// --- END TEMP LOGGING ---

				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					//_common.LogError($"VerifySession:END " + JsonConvert.SerializeObject(response), filePrefix);

					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
							x => x.Game == gameCode
					&& x.Token == rqdata.token);

					if (foundRecord == null)
					{
						// --- TEMP DIAGNOSTIC LOGGING ---
						var allTokensForGame = db.SeamlessWallets
							.Where(x => x.Game == gameCode)
							.Select(x => new { x.Username, x.Token })
							.ToList();
						_common.LogError(
							$"VerifySession:DEBUG -> TOKEN NOT FOUND for game={gameCode}, " +
							$"lookingFor={rqdata.token}. Current tokens in DB for this game: " +
							JsonConvert.SerializeObject(allTokensForGame),
							filePrefix);
						// --- END TEMP LOGGING ---

						response.error = "4";
						response.description = "Authenticate failed";
						//_common.LogError($"VerifySession:END " + JsonConvert.SerializeObject(response), filePrefix);
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}
					else
					{
						var foundRecord1 = db.SeamlessWallets.SingleOrDefault(
									x => x.Game == gameCode
							&& x.Username == foundRecord.Username);

						if (foundRecord1 == null)
						{
							response.error = "2";
							response.description = "Player not found";
							//_common.LogError($"VerifySession:END " + JsonConvert.SerializeObject(response), filePrefix);

							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
							};
						}
					}

					response.userId = foundRecord.Username;
					response.currency = foundRecord.Currency;
					response.cash = Math.Round(foundRecord.Cash + foundRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					//_common.LogError($"VerifySession:END " + JsonConvert.SerializeObject(response), filePrefix);

					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:VerifySession:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("VerifySession", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage Result(PragmaticCreditRequest rqdata)
		{
			Common _common = new Common();
			PragmaticCreditResponse response = new PragmaticCreditResponse();
			try
			{
				var param = "";

				param = "amount=" + rqdata.amount + "&gameId=" + rqdata.gameId + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
								+ "&roundDetails=" + rqdata.roundDetails + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;
				if (!string.IsNullOrEmpty(rqdata.bonusCode))
				{
					param = "amount=" + rqdata.amount + "&bonusCode=" + rqdata.bonusCode + "&gameId=" + rqdata.gameId + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
							+ "&roundDetails=" + rqdata.roundDetails + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;
				}

				if (!string.IsNullOrEmpty(rqdata.promoWinReference))
				{
					param = "amount=" + rqdata.amount + "&gameId=" + rqdata.gameId
									+ "&promoCampaignID=" + rqdata.promoCampaignID + "&promoCampaignType=" + rqdata.promoCampaignType
									+ "&promoWinAmount=" + rqdata.promoWinAmount + "&promoWinReference=" + rqdata.promoWinReference
									+ "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
							+ "&roundDetails=" + rqdata.roundDetails + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;
				}

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					var credit = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					if (credit == null)
					{
						try
						{
							var b = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							var balanceBefore = b.Cash + b.Chip;
							b.Cash = b.Cash + rqdata.amount;
							db.SaveChanges();

							var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							SeamlessGameCredit obj = new SeamlessGameCredit()
							{
								Id = rqdata.reference,
								PlayerID = rqdata.userId,
								BalanceBefore = balanceBefore,
								Amount = rqdata.amount,
								BalanceAfter = c.Cash + c.Chip,
								GameID = gameCode,
								Remark = "Payout " + rqdata.reference,
								CreatedDt = DateTime.Now
							};
							db.SeamlessGameCredits.Add(obj);
							db.SaveChanges();

							if (seamlessFlag == true)
							{
								DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0);
								var foundGame = db.PPSlotGameNews.SingleOrDefault(x => x.Id == rqdata.roundId && x.PlayerID == rqdata.userId);
								if (foundGame != null)
								{
									foundGame.Payout = rqdata.amount;
									foundGame.WinLose = rqdata.amount - foundGame.Turnover;
									foundGame.PayoutDt = start.AddMilliseconds(rqdata.timestamp);
								}
								db.SaveChanges();
							}


							//credit promo amount too
							if (!string.IsNullOrEmpty(rqdata.promoWinReference))
							{
								var promo = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.promoWinReference).FirstOrDefault();
								if (promo == null)
								{
									try
									{
										var a = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
										var pbalanceBefore = a.Cash + a.Chip;
										a.Cash = a.Cash + rqdata.promoWinAmount;
										db.SaveChanges();

										var d = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
										SeamlessGameCredit pobj = new SeamlessGameCredit()
										{
											Id = rqdata.promoWinReference,
											PlayerID = rqdata.userId,
											BalanceBefore = pbalanceBefore,
											Amount = rqdata.promoWinAmount,
											BalanceAfter = d.Cash + d.Chip,
											GameID = gameCode,
											Remark = "Promo Payout " + rqdata.promoWinReference,
											CreatedDt = DateTime.Now
										};
										db.SeamlessGameCredits.Add(pobj);
										db.SaveChanges();
									}
									catch
									{
										response.error = "-3034";
										response.description = "Payout failed";
										return new HttpResponseMessage()
										{
											StatusCode = HttpStatusCode.OK,
											Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
										};
									}
								}
							}
						}
						catch
						{
							response.error = "-3034";
							response.description = "Payout failed";
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
							};
						}
					}

					var newRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//if it is duplicate credit record, just get current player balance and return 
					response.transactionId = rqdata.reference;
					response.currency = newRecord.Currency;
					response.cash = Math.Round(newRecord.Cash + newRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:Result:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("Result", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage Bet(PragmaticDebitRequest rqdata)
		{
			Common _common = new Common();
			PragmaticDebitResponse response = new PragmaticDebitResponse();
			try
			{
				var param = "amount=" + rqdata.amount + "&gameId=" + rqdata.gameId + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
									+ "&roundDetails=" + rqdata.roundDetails + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;

				if (!String.IsNullOrEmpty(rqdata.bonusCode))
				{
					param = "amount=" + rqdata.amount + "&bonusCode=" + rqdata.bonusCode + "&gameId=" + rqdata.gameId + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
								+ "&roundDetails=" + rqdata.roundDetails + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;
				}

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					var debit = db.SeamlessGameDebits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					if (debit != null)
					{
						//response.error = "1";
						//response.description = "Duplicate Transaction";
						//return new HttpResponseMessage()
						//{
						//	StatusCode = HttpStatusCode.OK,
						//	Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						//};
					}
					else
					{
						var newRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
						var balanceBefore = newRecord.Cash + newRecord.Chip;
						if (balanceBefore < rqdata.amount)
						{
							response.error = "1";
							response.description = "Insufficient balance";
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
							};
						}

						if (rqdata.amount > newRecord.Chip)
						{
							newRecord.Cash = newRecord.Cash - (rqdata.amount - newRecord.Chip);
							newRecord.Chip = 0;
							db.SaveChanges();
						}
						else
						{
							newRecord.Chip = newRecord.Chip - rqdata.amount;
							db.SaveChanges();
						}

						var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
						SeamlessGameDebit obj = new SeamlessGameDebit()
						{
							Id = rqdata.reference,
							PlayerID = rqdata.userId,
							BalanceBefore = balanceBefore,
							Amount = rqdata.amount,
							BalanceAfter = c.Cash + c.Chip,
							GameID = gameCode,
							Remark = "Bet " + rqdata.reference,
							CreatedDt = DateTime.Now
						};
						db.SeamlessGameDebits.Add(obj);
						db.SaveChanges();

						if (seamlessFlag == true)
						{
							PPSlotGameNew game = new PPSlotGameNew()
							{
								Id = rqdata.roundId,
								PlayerID = rqdata.userId,
								Turnover = rqdata.amount,
								GameID = rqdata.gameId,
								CreatedDt = DateTime.Now
							};
							db.PPSlotGameNews.Add(game);
							db.SaveChanges();
						}

					}

					var final = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					response.transactionId = rqdata.reference;
					response.currency = final.Currency;
					response.cash = Math.Round(final.Cash + final.Chip, 2);
					response.bonus = 0;
					response.usedPromo = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:Bet:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("Bet", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage Refund(PragmaticRollbackRequest rqdata)
		{
			Common _common = new Common();
			PragmaticRollbackResponse response = new PragmaticRollbackResponse();
			try
			{
				var param = "providerId=" + rqdata.providerId + "&reference=" + rqdata.reference + "&userId=" + rqdata.userId;

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					var credit = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					if (credit == null)
					{
						var debit = db.SeamlessGameDebits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
						//got deduct money using this reference number before
						if (debit != null)
						{
							var b = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							var balanceBefore = b.Cash + b.Chip;
							b.Cash = b.Cash + (decimal)debit.Amount;
							db.SaveChanges();

							var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							SeamlessGameCredit obj = new SeamlessGameCredit()
							{
								Id = rqdata.reference,
								PlayerID = rqdata.userId,
								BalanceBefore = balanceBefore,
								Amount = debit.Amount,
								BalanceAfter = c.Cash + c.Chip,
								GameID = gameCode,
								Remark = "Refund " + rqdata.reference,
								CreatedDt = DateTime.Now
							};
							db.SeamlessGameCredits.Add(obj);
							db.SaveChanges();
						}
					}

					response.transactionId = rqdata.reference;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:Result:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("Result", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage BonusWin(PragmaticCreditRequest rqdata)
		{
			Common _common = new Common();
			PragmaticCreditResponse response = new PragmaticCreditResponse();
			try
			{
				var param = "";

				param = "amount=" + rqdata.amount + "&bonusCode=" + rqdata.bonusCode + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
										+ "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					//var credit = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					//if (credit == null)
					//{
					//	try
					//	{
					//		var b = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//		var balanceBefore = b.Cash + b.Chip;
					//		b.Cash = b.Cash + rqdata.amount;
					//		db.SaveChanges();

					//		var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//		SeamlessGameCredit obj = new SeamlessGameCredit()
					//		{
					//			Id = rqdata.reference,
					//			PlayerID = rqdata.userId,
					//			BalanceBefore = balanceBefore,
					//			Amount = rqdata.amount,
					//			BalanceAfter = c.Cash + c.Chip,
					//			GameID = gameCode,
					//			Remark = "Bonus Win " + rqdata.reference,
					//			CreatedDt = DateTime.Now
					//		};
					//		db.SeamlessGameCredits.Add(obj);
					//		db.SaveChanges();
					//	}
					//	catch
					//	{
					//		response.error = "-3034";
					//		response.description = "Payout failed";
					//		return new HttpResponseMessage()
					//		{
					//			StatusCode = HttpStatusCode.OK,
					//			Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					//		};
					//	}
					//}

					var newRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//if it is duplicate credit record, just get current player balance and return 
					response.transactionId = rqdata.reference;
					response.currency = newRecord.Currency;
					response.cash = Math.Round(newRecord.Cash + newRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:BonusWin:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("BonusWin", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage JackpotWin(PragmaticCreditRequest rqdata)
		{
			Common _common = new Common();
			PragmaticCreditResponse response = new PragmaticCreditResponse();
			try
			{
				var param = "";

				param = "amount=" + rqdata.amount + "&gameId=" + rqdata.gameId + "&jackpotId=" + rqdata.jackpotId + "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference
									 + "&roundId=" + rqdata.roundId + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					var credit = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					if (credit == null)
					{
						try
						{
							var b = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							var balanceBefore = b.Cash + b.Chip;
							b.Cash = b.Cash + rqdata.amount;
							db.SaveChanges();

							var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							SeamlessGameCredit obj = new SeamlessGameCredit()
							{
								Id = rqdata.reference,
								PlayerID = rqdata.userId,
								BalanceBefore = balanceBefore,
								Amount = rqdata.amount,
								BalanceAfter = c.Cash + c.Chip,
								GameID = gameCode,
								Remark = "Jackpot Win " + rqdata.reference,
								CreatedDt = DateTime.Now
							};
							db.SeamlessGameCredits.Add(obj);
							db.SaveChanges();
						}
						catch
						{
							response.error = "-3034";
							response.description = "Payout failed";
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
							};
						}
					}

					var newRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//if it is duplicate credit record, just get current player balance and return 
					response.transactionId = rqdata.reference;
					response.currency = newRecord.Currency;
					response.cash = Math.Round(newRecord.Cash + newRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:JackpotWin:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("JackpotWin", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}

		[HttpPost]
		public HttpResponseMessage PromoWin(PragmaticCreditRequest rqdata)
		{
			Common _common = new Common();
			PragmaticCreditResponse response = new PragmaticCreditResponse();
			try
			{
				var param = "";

				param = "amount=" + rqdata.amount + "&campaignId=" + rqdata.campaignId + "&campaignType=" + rqdata.campaignType + "&currency=" + rqdata.currency
										+ "&providerId=" + rqdata.providerId + "&reference=" + rqdata.reference + "&timestamp=" + rqdata.timestamp + "&userId=" + rqdata.userId;

				var compareParam = GetMD5Signature(param);
				if (rqdata.hash != compareParam)
				{
					response.error = "5";
					response.description = "Invalid hash code";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}

				using (BingoConn db = new BingoConn())
				{
					var foundRecord = db.SeamlessWallets.SingleOrDefault(
								x => x.Game == gameCode
							&& x.Username == rqdata.userId);

					if (foundRecord == null)
					{
						response.error = "2";
						response.description = "Player not found";
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
						};
					}

					var credit = db.SeamlessGameCredits.Where(x => x.GameID == gameCode && x.Id == rqdata.reference).FirstOrDefault();
					if (credit == null)
					{
						try
						{
							var b = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							var balanceBefore = b.Cash + b.Chip;
							b.Cash = b.Cash + rqdata.amount;
							db.SaveChanges();

							var c = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
							SeamlessGameCredit obj = new SeamlessGameCredit()
							{
								Id = rqdata.reference,
								PlayerID = rqdata.userId,
								BalanceBefore = balanceBefore,
								Amount = rqdata.amount,
								BalanceAfter = c.Cash + c.Chip,
								GameID = gameCode,
								Remark = "Tournament Win " + rqdata.reference,
								CreatedDt = DateTime.Now
							};
							db.SeamlessGameCredits.Add(obj);
							db.SaveChanges();
						}
						catch
						{
							response.error = "-3034";
							response.description = "Payout failed";
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
							};
						}
					}

					var newRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameCode && x.Username == rqdata.userId);
					//if it is duplicate credit record, just get current player balance and return 
					response.transactionId = rqdata.reference;
					response.currency = newRecord.Currency;
					response.cash = Math.Round(newRecord.Cash + newRecord.Chip, 2);
					response.bonus = 0;
					response.error = "0";
					response.description = "Success";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
					};
				}
			}
			catch (Exception ex)
			{
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				p_ErrorMessage = ex.Message.ToString();
				_common.LogError($"{dateTime}:PromoWin:EXCEPTION " + string.Format("{0}", gameCode) + " " + ex.Message.ToString(), filePrefix);
				_common.InsertLogWithGameId("PromoWin", gameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "request", JsonConvert.SerializeObject(rqdata));
				response.error = "120";
				response.description = "Internal server error";
				return new HttpResponseMessage()
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
				};
			}
		}
	}
}
