using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming.CasinoDailyReport
{
	public class CasinoDailyReportResponse
	{
		public List<Datum> data {get; set;}
		public string timestamp {get; set;}
		public string uuid {get; set;}
	}

	public class Datum
	{
		public string currency {get; set;}
		public string date {get; set;}
		public decimal payout {get; set;}
		public int roundCount {get; set;}
		public Table table {get; set;}
		public string gameType {get; set;}
		public decimal wager {get; set;}
	}

	public class Table
	{
		public string id {get; set;}
		public string name {get; set;}
	}
}
