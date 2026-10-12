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
using System.Data.Entity.Validation;
using System.Web.Script.Serialization;

namespace GameRecord.Controllers
{
	public class PTController : Controller
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
		private string P_BackUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(System.Web.HttpContext.Current.Request.Url.PathAndQuery, "/") + "PT/SlotLobby";
		private string P_HostUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(System.Web.HttpContext.Current.Request.Url.PathAndQuery, "/");

		public ActionResult Live()
		{
			return View();
		}

		public ActionResult Slot()
		{
			return View();
		}

		//http://localhost:51148/PT/SlotLobby
		public ActionResult SlotLobby(string playerName, string lang="en")
		{
			PTSlotLobbyVM model = new PTSlotLobbyVM
			{
				playerName = playerName,
				lang = lang,
				gameList = GetGameList(null, lang, null),
			};
			return View(model);
		}

		public JsonResult SearchGame(string gameName, string lang, string gameCategory)
		{
			List<PTSlotLobbyGame> gameList = GetGameList(gameName, lang, gameCategory);
			return Json(gameList, JsonRequestBehavior.AllowGet);
		}

		private List<PTSlotLobbyGame> GetGameList(string gameName, string lang, string gameCategory)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var q_list = db.PTSlotLobbyGames.Where(x => true);

					if (!string.IsNullOrWhiteSpace(gameName))
                    {
						if (lang?.ToUpper() == "EN")
							q_list = q_list.Where(x => x.GameNameEn.Contains(gameName));
						else
							q_list = q_list.Where(x => x.GameNameCn.Contains(gameName));
					}

					if (gameCategory == "Hot")
						q_list = q_list.Where(x => x.IsHot == true).OrderByDescending(x => x.HotSeq);
					else if (gameCategory == "Favourite")
						q_list = q_list.Where(x => x.IsFavorite == true).OrderByDescending(x => x.FavoriteSeq);
					else if (gameCategory == "New")
						q_list = q_list.Where(x => x.IsNew == true).OrderByDescending(x => x.NewSeq);
					else
						q_list = q_list.Where(x => x.IsAll == true).OrderByDescending(x => x.AllSeq);

					return q_list.ToList();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at PT/GetGameList: " + ex.Message.ToString(), P_GamePrefix);
				return null;
			}
		}

		public string LaunchSlot(string playerName, string gameID, string lang = "en")
		{
			lock (MyLock)
			{
				try
				{
					string url = P_HostUrl + "PT/Slot";

					if (string.IsNullOrWhiteSpace(lang))
						lang = "en";

					string back = string.Format("{0}?playerName={1}&lang={2}", P_BackUrl, playerName, lang);

					url = url + string.Format("?playerName={0}&gameID={1}&lang={2}", (playerName.Trim()).ToUpper(), gameID, lang);

					//InsertLog(playerName, "LaunchSlot", "LaunchSlot_Request", url, "playerName", playerName, "gameID", gameID, "lang", lang, "back", HttpUtility.UrlEncode(back));

					dynamic res = new ExpandoObject();
					res.status = 1;
					res.message = url;

					//InsertLog(playerName, "LaunchSlot", "LaunchSlot_Response", JsonConvert.SerializeObject(res), "playerName", playerName, "gameID", gameID, "lang", lang, "back", HttpUtility.UrlEncode(back));

					JsonResult json = new JsonResult
					{
						Data = JsonConvert.SerializeObject(res)
					};

					return new JavaScriptSerializer().Serialize(json);
				}
				catch (WebException ex)
				{
					_common.LogError("LaunchSlot" + " " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}", playerName, lang, gameID) + " " + ex.Message.ToString(), "PT");
					//InsertLog(playerName, "LaunchSlot", "Exception", ex.Message.ToString(), "playerName", playerName, "gameID", gameID, "lang", lang);
					return null;
				}
			}
		}
	}
}