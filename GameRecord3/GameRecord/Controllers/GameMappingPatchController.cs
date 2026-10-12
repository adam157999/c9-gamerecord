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
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class GameMappingPatchController : Controller
	{
		public static readonly object MyLock = new object();
		Common _common = new Common();

		public bool PatchGameID()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var gmArr = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BKA").Select(x => x.GamePlayerID.ToUpper().Trim()).Distinct().ToArray();

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						con.Open();
						using (SqlDataAdapter adapter = new SqlDataAdapter("select * from PLAYER_Game where INT1 = 3", con))
						{
							DataTable table = new DataTable();
							adapter.Fill(table);

							var twList = table.AsEnumerable().ToList().Where(s => !gmArr.Contains(s.Field<string>("STR1").ToUpper().Trim())).ToList();

							if (twList != null)
							{
								foreach (var t in twList)
								{
									var playerId = t["KEY1"].ToString().Trim().ToUpper();
									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BKA" && x.PlayerID.ToUpper() == playerId).FirstOrDefault();

									if (record == null)
									{
										GameMapping gm = new GameMapping()
										{
											UID = Guid.NewGuid().ToString(),
											Game = "BKA",
											GamePlayerID = t["STR1"].ToString().Trim(),
											PlayerID = t["KEY1"].ToString().Trim(),
											Currency = "MYR",
											CreatedDate = DateTime.Now,
											IsActive = true
										};
										db.GameMappings.Add(gm);
									}

									//if (record != null)
									//{
									//	gm.UID = record.UID;
									//	gm.CreatedDate = record.CreatedDate;
									//	gm.ModifiedDate = DateTime.Now;
									//}
									//else
									//{
									//	gm.UID = Guid.NewGuid().ToString();
									//	gm.CreatedDate = DateTime.Now;
									//}
									//db.GameMappings.AddOrUpdate(gm);
								}
								db.SaveChanges();
							}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("PatchGameID" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GMPatch");
				return false;
			}
		}

		public void PatchJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = PatchGameID();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "GMPatch");
			}
		}
	}
}