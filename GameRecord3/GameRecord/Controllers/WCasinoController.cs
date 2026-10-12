using GameRecord.Controllers.V2;
using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
    public class WCasinoController : Controller
    {
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "WCASINO";
		Common _common = new Common();
		private WCasinoConnector api = new WCasinoConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//http://localhost:51148/WCasino/GetBetLimitInterface
		//https://c9live-gamerecord.azurewebsites.net/WCasino/GetBetLimitInterface
		//https://c9live-gamerecord-qa.azurewebsites.net/WCasino/GetBetLimitInterface
		public string GetBetLimitInterface()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					api = new WCasinoConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					var result = api.BetLimitInterface();

					return JsonConvert.SerializeObject(result);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at WCasino/GetBetLimitInterface: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at WCasino/GetBetLimitInterface: " + ex.Message.ToString();
			}
		}
	}
}