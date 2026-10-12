using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GameRecord.Models;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameRecord.Controllers.V2;

namespace GameRecord.Controllers
{
	public class DemoController : Controller
	{
		[System.Web.Http.HttpPost]
		public ActionResult TestStaging(DemoStagingModel model)
		{
			ViewBag.Title = "Test Provider (Staging)";
			ViewBag.Message = "";
			ViewBag.Provider = CatchData.GetApiKeysByEnv("STAG");
			ViewBag.Data = null;

			if (model != null)
			{
				ViewBag.Data = model;

				if (!string.IsNullOrEmpty(ViewBag.Data.provider))
				{
					ViewBag.Data.providerConfig = new JObject();

					var providerConfig = CatchData.GetApiKeys(ViewBag.Data.provider, "STAG")?.ApiConfig;
					if (!string.IsNullOrEmpty(providerConfig))
					{
						ViewBag.Data.providerConfig = JObject.Parse(providerConfig);
					}
					else
					{
						ViewBag.Data.providerConfig = null;
					}

					BaseConnector api;
					string TrackerId = Guid.NewGuid().ToString();
					if (!string.IsNullOrEmpty(model.btnRegister))
					{
						api = GetConnector(ViewBag.Data.provider, model.playerId1, model.currency, BaseConnector.GameEnvironment.STAG);
						var res = api.CreatePlayer(model.playerId1, model.password1, model.playerName1, "");
						if (res.Code == 0)
						{
							ViewBag.Data.success1 = true;
						}
						else
						{
							ViewBag.Data.success1 = false;
						}
						ViewBag.Data.status1 = res.Message;
						ViewBag.Data.request1 = api.request;
						ViewBag.Data.response1 = api.response;
						ViewBag.Data.anchor = "btnRegister";
					}
					else if (!string.IsNullOrEmpty(model.btnGetBalance))
					{
						api = GetConnector(ViewBag.Data.provider, model.playerId2, model.currency, BaseConnector.GameEnvironment.STAG);
						var res = api.GetBalance();
						if (res.Code == 0)
						{
							ViewBag.Data.success2 = true;
						}
						else
						{
							ViewBag.Data.success2 = false;
						}
						ViewBag.Data.status2 = res.Message;
						ViewBag.Data.request2 = api.request;
						ViewBag.Data.response2 = api.response;
						ViewBag.Data.anchor = "btnGetBalance";
					}
					else if (!string.IsNullOrEmpty(model.btnDeposit))
					{
						api = GetConnector(ViewBag.Data.provider, model.playerId3, model.currency, BaseConnector.GameEnvironment.STAG);
						BaseConnector.Error err = null;
						bool success = api.GameDeposit(model.amount3, TrackerId, ref err);
						ViewBag.Data.success3 = success;

                        if (!success)
                        {
                            ViewBag.Data.status3 = err.ErrorDesc;
                        }

                        ViewBag.Data.request3 = api.request;
                        ViewBag.Data.response3 = api.response;
                        ViewBag.Data.anchor = "btnDeposit";
					}
					else if (!string.IsNullOrEmpty(model.btnWithdraw))
					{
						api = GetConnector(ViewBag.Data.provider, model.playerId4, model.currency, BaseConnector.GameEnvironment.STAG);
						BaseConnector.Error err = null;

                        bool success = api.GameWithdraw(model.amount4, TrackerId, ref err);
                        ViewBag.Data.success4 = success;

                        if (!success)
                        {
                            ViewBag.Data.status4 = err.ErrorDesc;
                        }

                        ViewBag.Data.request4 = api.request;
                        ViewBag.Data.response4 = api.response;
                        ViewBag.Data.anchor = "btnWithdraw";
                    }
					else if (!string.IsNullOrEmpty(model.btnOpenGame))
					{
						bool isMobile = model.device5 == "Mobile" ? true : false;
						api = GetConnector(ViewBag.Data.provider, model.playerId5, model.currency, BaseConnector.GameEnvironment.STAG);
						var res = api.GetGameUrl(model.language5, isMobile);
						if (res.Code == 0)
						{
							ViewBag.Data.success5 = true;
						}
						else
						{
							ViewBag.Data.success5 = false;
						}
						ViewBag.Data.status5 = res.Message;
						ViewBag.Data.url5 = res.Url;
						ViewBag.Data.request5 = api.request;
						ViewBag.Data.response5 = api.response;
						ViewBag.Data.anchor = "btnOpenGame";
					}
					else if (!string.IsNullOrEmpty(model.btnGetBet))
					{
						if (ViewBag.Data.provider == "BT28WIN")
						{
							try
							{
								var ctrl = new BT28WinController(BaseConnector.GameEnvironment.STAG);
								ctrl.BT28WinJob();
								ViewBag.Data.success6 = true;
							}
							catch
							{
								ViewBag.Data.success6 = false;
							}
						}
						else if (ViewBag.Data.provider == "EVOLUTIONGAMING")
						{
							try
							{
								var ctrl = new EvolutionController(BaseConnector.GameEnvironment.STAG);
								ctrl.EvoJob();
								ViewBag.Data.success6 = true;
							}
							catch
							{
								ViewBag.Data.success6 = false;
							}
						}
						ViewBag.Data.anchor = "btnGetBet";
					}
				}
			}
			else
			{
				ViewBag.Data = new DemoStagingModel();
				ViewBag.Data.providerConfig = new JObject();
			}

			return View();
		}

		public string GetIPAddress()
		{
			var result = string.Empty;
			System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
			string forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			string remoteAddr = string.IsNullOrWhiteSpace(forwardedFor) ? request.ServerVariables["REMOTE_ADDR"] : forwardedFor.Split(',').Select(s => s.Trim()).First();
			result = String.IsNullOrWhiteSpace(remoteAddr) ? request.UserHostAddress : remoteAddr;
			return result;
		}

		public BaseConnector GetConnector(string GameID, string PlayerID, string Currency, BaseConnector.GameEnvironment env = BaseConnector.GameEnvironment.PROD)
		{
			if (string.IsNullOrEmpty(GameID)) return null;

			var ip = GetIPAddress();
			switch (GameID)
			{
				case "BG":
					return null;
				case "AG":
					return new AGConnector(PlayerID, Currency, ip, env);
				case "PT":
					return new PTConnector(PlayerID, Currency, ip, env);
				case "IG":
					return null;
				case "BT28WIN":
					return new BT28WINConnector(PlayerID, Currency, ip, env);
				case "ALLCITY":
					return null;
				case "IBC":
					return new IBCConnector(PlayerID, Currency, ip, env);
				case "EVO888":
					return null;
				case "VPOWER":
					return null;
				case "SEXYGAMING":
					return new AESexyConnector(PlayerID, Currency, ip, env);
				case "SPADEGAMING":
					return null;
				case "MEGA":
					return new MEGAConnector(PlayerID, Currency, ip, env);
				case "EVOLUTIONGAMING":
					return new EVOGMConnector(PlayerID, Currency, ip, env);
				case "MENANG88":
					return null;
				case "XE88":
					return null;
				case "KYPOKER":
					return null;
				case "SABAESPORTS":
					return null;
				case "TPG":
					return null;
				case "TPGFISH":
					return null;
				case "VGAMING":
					return null;
				case "CQ9":
					return new CQ9Connector(PlayerID, Currency, ip, env);
				case "KISS918":
					return new KISS918Connector(PlayerID, Currency, ip, env);
				case "SAG":
					return new SAGConnector(PlayerID, Currency, ip, env);
				case "MKY888":
					return new MKY888Connector(PlayerID, Currency, ip, env);
				case "HABANERO":
					return new HabaneroConnector(PlayerID, Currency, ip, env);
				case "JILI":
					return new JiLiConnector(PlayerID, Currency, ip, env);
				case "PRAGMATICSLOT":
					return new PragmaticSlotConnector(PlayerID, Currency, ip, env);
				case "PRAGMATICLIVE":
					return new PragmaticLiveConnector(PlayerID, Currency, ip, env);
				case "DG":
					return new DGConnector(PlayerID, Currency, ip, env);
				case "GFG":
					return new GFGConnector(PlayerID, Currency, ip, env);
				case "PTSLOT":
					return new PTConnector(PlayerID, Currency, ip, env);
                case "PTV2SLOT":
                    return new PTV2SlotConnector(PlayerID, Currency, ip, env);
                case "PTV2LIVE":
                    return new PTV2LiveConnector(PlayerID, Currency, ip, env);
                default:
					return null;
			}
		}
	}
}