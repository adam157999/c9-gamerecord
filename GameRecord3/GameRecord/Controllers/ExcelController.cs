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
using OfficeOpenXml;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.Storage.Blob;
using OfficeOpenXml.Style;
using System.Configuration;
using System.Drawing;
using Microsoft.Azure.Storage;

namespace GameRecord.Controllers
{
	public class ExcelController : Controller
	{
		public FileResult Excel()
		{
			DateTime Date = Convert.ToDateTime("2021-02-01");
			var fileName = $"VPOWER_{Date.ToString("yyyyMM")}.xlsx";

			CloudStorageAccount storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"]);
			CloudBlobClient client = storage.CreateCloudBlobClient();
			CloudBlobContainer container = client.GetContainerReference("bingotown-cashin-reports");

			using (MemoryStream stream = new MemoryStream())
			{
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
				using (var package = new ExcelPackage(stream))
				{
					while (Date <= new DateTime(2021, 02, 28))
					{
						ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(Date.ToString("yyyy-MM-dd"));
						worksheet.Cells[1, 1].Value = "Player";
						worksheet.Cells[1, 2].Value = "DateTime";

						using (var range = worksheet.Cells[1, 1, 1, 2])
						{
							range.Style.Font.Bold = true;
							range.Style.Fill.PatternType = ExcelFillStyle.Solid;
							range.Style.Fill.BackgroundColor.SetColor(Color.Black);
							range.Style.Font.Color.SetColor(Color.White);
						}

						using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["bingotown_app2"].ConnectionString))
						{
							con.Open();
							//allcity
							//string sql = "select * from TrackerAC where type = 'GetGameApk' and field2 = 'GetGameApk_Request' ";
							//sql += $"and CAST(CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by CreatedDate ";

							//ag
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerAG a left join GameMapping b on a.Value4 = b.GamePlayerID and Game = 'AG' ";
							//sql += "where type = 'ForwardGame' and field2 = 'ForwardGame' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//bg
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerBG a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'BG' ";
							//sql += "where type = 'login' and field2 = 'Login_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//28win
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerBT28Win a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'BT28Win' ";
							//sql += "where type = 'Bet' and field2 = 'Bet_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//evo888
							//string sql = "select '' PlayerID, CreatedDate from TrackerEvo888 where type = 'GetGameApk' and field2 = 'GetGameApk_Request' ";
							//sql += $"and CAST(CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by CreatedDate";

							//ibc
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerIBC a left join GameMapping b on a.Value4 = b.GamePlayerID and Game = 'IBC' ";
							//sql += "where type = 'LaunchGame' and field2 = 'LaunchGame_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//play8oy
							//string sql = "select b.PlayerID, a.CreatedDate, a.Value3 from TrackerIG a cross apply openjson (a.value2) with( PlayerAccount varchar(100)) as Projects ";
							//sql += "left join GameMapping b on REPLACE(PlayerAccount, 'bt_', '') = b.GamePlayerID and Game = 'IG' ";
							//sql += "where type = 'transfer' and field2 = 'Transfer_Request' and convert(decimal(10,2), IIF(ISNUMERIC(value3) = 1, value3, '0')) > 0 ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//playtech
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerPT a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'PT' ";
							//sql += "where type = 'LaunchGame' and field2 = 'LaunchGame_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//sexygaming
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerSexy a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'SEXYGAMING' ";
							//sql += "where type = 'GetGameUrl' and field2 = 'GetGameUrl_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//spadegaming
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerSpade a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'SPADEGAMING' ";
							//sql += "where type = 'LaunchGame' and field2 = 'LaunchGame_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//tiger
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerTPG a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'TPG' ";
							//sql += "where type = 'LaunchLobby' and field2 = 'LaunchLobby_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//tigerfish
							//string sql = "select b.PlayerID, a.CreatedDate from TrackerTPGFish a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'TPGFISH' ";
							//sql += "where type = 'LaunchLobby' and field2 = 'LaunchLobby_Request' ";
							//sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							//sql += "order by a.CreatedDate ";

							//vpower
							string sql = "select b.PlayerID, a.CreatedDate from TrackerVPower a left join GameMapping b on a.Value3 = b.GamePlayerID and Game = 'VPOWER' ";
							sql += "where type = 'LaunchLobby' and field2 = 'LaunchLobby_Request' ";
							sql += $"and CAST(a.CreatedDate AS DATE) = '{Date.ToString("yyyy-MM-dd")}' ";
							sql += "order by a.CreatedDate ";

							using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
							{
								DataTable table = new DataTable();
								adapter.Fill(table);
								if (table.Rows.Count > 0)
								{
									int rowNumber = 2;
									foreach (DataRow row in table.Rows)
									{
										worksheet.Cells[rowNumber, 1].Value = row["PlayerID"].ToString(); //id
										worksheet.Cells[rowNumber, 2].Value = Convert.ToDateTime(row["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"); //orderid
										rowNumber++;
									}
									worksheet.Cells["A1:B" + (rowNumber - 1).ToString()].AutoFilter = true;
								}
							}
						}

						for (int i = 1; i <= 2; i++)
							worksheet.Column(i).AutoFit();

						Date = Date.AddDays(1);
					}


					package.Workbook.Properties.Title = "VPOWER";
					package.Workbook.Properties.Author = "Man City";
					package.Workbook.Properties.Company = "Man City";
					package.Save();

					CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
					stream.Seek(0, SeekOrigin.Begin);
					blockBlob.UploadFromStream(stream);

					Stream blobStream = blockBlob.OpenRead();
					return File(blobStream, blockBlob.Properties.ContentType, fileName);
				}
			}
		}
	}
}