using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
	public abstract class BaseConnector : IDisposable
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix;
		public Common _common = new Common();
		public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
		public string BingoConnReport = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConnReport"].ToString();
		public static string FilePrefix = "GAMEAPI";
		public string playerID = "";
		public string currencyID = "";
		public string ipAddr = "";
		public GameEnvironment environment;
		public string targetApiUrl;
		public string request;
		public string response;

		public BaseConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD)
		{
			playerID = PlayerID.ToUpper().Trim();
			currencyID = CurrencyID.ToUpper().Trim();
			ipAddr = IPAddr;
			environment = Environment;
		}

		#region Models
		public class Error
		{
			public string ErrorCode { get; set; }
			public string ErrorDesc { get; set; }
		}
		#endregion

		#region Main Function
		public bool InsertLog(string GameId, string sType, string sField1 = "", object sValue1 = null, string sField2 = "", object sValue2 = null, string sField3 = "", object sValue3 = null
														, string sField4 = "", object sValue4 = null, string sField5 = "", object sValue5 = null, string sField6 = "", object sValue6 = null
														, string sField7 = "", object sValue7 = null, string sField8 = "", object sValue8 = null, string sField9 = "", object sValue9 = null
														, string sField10 = "", object sValue10 = null)
		{
			try
			{
				string sID = Guid.NewGuid().ToString();
				DateTime dtNow = DateTime.Now;

				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameTracker_IUD", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("@Type", "0");
					cmd.Parameters.AddWithValue("@Id", sID);
					cmd.Parameters.AddWithValue("@GameID", GameId);
					cmd.Parameters.AddWithValue("@TrackerType", sType);
					cmd.Parameters.AddWithValue("@Field1", sField1);
					cmd.Parameters.AddWithValue("@Value1", sValue1 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field2", sField2);
					cmd.Parameters.AddWithValue("@Value2", sValue2 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field3", sField3);
					cmd.Parameters.AddWithValue("@Value3", sValue3 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field4", sField4);
					cmd.Parameters.AddWithValue("@Value4", sValue4 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field5", sField5);
					cmd.Parameters.AddWithValue("@Value5", sValue5 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field6", sField6);
					cmd.Parameters.AddWithValue("@Value6", sValue6 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field7", sField7);
					cmd.Parameters.AddWithValue("@Value7", sValue7 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field8", sField8);
					cmd.Parameters.AddWithValue("@Value8", sValue8 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field9", sField9);
					cmd.Parameters.AddWithValue("@Value9", sValue9 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Field10", sField10);
					cmd.Parameters.AddWithValue("@Value10", sValue10 ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@CreatedDate", dtNow);
					cmd.Parameters.AddWithValue("@Deleted", '0');

					cmd.ExecuteNonQuery();
				}

				return true;
			}
			catch
			{
				return false;
			}
		}
		public void InsertGameMapping(string gameID, string inGameID, string inGamePass, string mcUID)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameMapping_IUD", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@Game", gameID));
					cmd.Parameters.Add(new SqlParameter("@GamePlayerID", inGameID));
					cmd.Parameters.Add(new SqlParameter("@GamePassword", inGamePass));
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
					cmd.Parameters.Add(new SqlParameter("@Currency", currencyID));

					cmd.ExecuteNonQuery();
				}

				using (SqlConnection conReport = new SqlConnection(BingoConnReport))
				{
					conReport.Open();
					SqlCommand cmd = new SqlCommand("sp_GameMapping_IUD", conReport);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@Game", gameID));
					cmd.Parameters.Add(new SqlParameter("@GamePlayerID", inGameID));
					cmd.Parameters.Add(new SqlParameter("@GamePassword", inGamePass));
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
					cmd.Parameters.Add(new SqlParameter("@Currency", currencyID));

					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public bool IsBlockedPlayer(string gameID, string playerID)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameBlockedPlayer_Sel", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@GameID", gameID));
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));

					DataTable table = new DataTable();
					SqlDataAdapter da = new SqlDataAdapter(cmd);
					da.Fill(table);

					if (table.Rows.Count > 0)
					{
						if (Convert.ToBoolean(table.Rows[0][0]) == false)
							return false;
						else
							return true;
					}
					else
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				return true;
			}
		}
		public GameMapping LoadGameMap(string gameID, string playerID, string currencyID)
		{
			GameMapping gameMap = null;

			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameMapping_Sel", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@GameID", gameID));
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
					cmd.Parameters.Add(new SqlParameter("@Currency", currencyID));

					SqlDataAdapter da = new SqlDataAdapter(cmd);
					DataTable dt = new DataTable();
					da.Fill(dt);

					if (dt != null && dt.Rows.Count > 0)
					{
						gameMap = dt.AsEnumerable().Select(x => new GameMapping()
						{
							UID = x.Field<string>("UID"),
							Game = x.Field<string>("Game"),
							GamePlayerID = x.Field<string>("GamePlayerID"),
							GamePassword = x.Field<string>("GamePassword"),
							PlayerID = x.Field<string>("PlayerID"),
							Currency = x.Field<string>("Currency"),
							ModifiedDate = x.Field<DateTime>("ModifiedDate"),
							CreatedDate = x.Field<DateTime>("CreatedDate"),
							IsActive = x.Field<bool>("IsActive")
						}).FirstOrDefault();
					}
					//return cmd.ExecuteScalar().ToString();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return gameMap;
		}
		public string GetGameLanguage(string gameID, string langId)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameLanguage_Sel", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@GameID", gameID));
					cmd.Parameters.Add(new SqlParameter("@LangID", langId));

					return cmd.ExecuteScalar().ToString();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
		public string GenerateAccessToken(string gameID, string playerID)
		{
			string token = "";

			try
			{
				token = Guid.NewGuid().ToString();
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameToken_IUD", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Type", "0"));
					cmd.Parameters.Add(new SqlParameter("@GameID", gameID));
					cmd.Parameters.Add(new SqlParameter("@PlayerID", playerID));
					cmd.Parameters.Add(new SqlParameter("@Token", token));

					token = cmd.ExecuteScalar().ToString();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return token;
		}
		#endregion

		#region Abstract Functions
		public abstract vm.GetBalanceVM GetBalance();
		public abstract vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID);
		public abstract vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = true);
		public abstract decimal? GetGameBalance();
		public abstract bool GameDeposit(decimal Amount, string TrackerId, ref Error Error);
		public abstract string RetryDeposit(decimal Amount, string TrackerId, ref Error Error);
		public abstract bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error);
		public abstract string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error);
		public abstract bool KickPlayer(string GamePlayerId, ref Error Error);
		public void Dispose()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Enum
		public enum GameEnvironment
		{
			PROD = 0,
			STAG = 1,
			DEV = 2
		}
		#endregion
	}
}