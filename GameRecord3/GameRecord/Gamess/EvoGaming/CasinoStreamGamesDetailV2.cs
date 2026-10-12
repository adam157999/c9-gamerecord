using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.CasinoStreamGamesDetailV2
{
	//Public Class Dealer
	//    Public Property uid As String
	//    Public Property name As String
	//End Class

	//Public Class SicBoCombo4And6
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoTriple1
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoCombo3And5
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoDouble4
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoCombo1And3
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoCombo3And6
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBoTotal11
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBo2
	//    Public Property multiplier As Integer
	//    Public Property stakeType As String
	//End Class

	//Public Class SicBoTotal9
	//    Public Property multiplier As Integer
	//End Class

	//Public Class SicBo5
	//    Public Property multiplier As Integer
	//    Public Property stakeType As String
	//End Class

	//Public Class SicBo4
	//    Public Property multiplier As Integer
	//    Public Property stakeType As String
	//End Class

	//Public Class LuckyNumbers
	//    Public Property __invalid_name__17 As Integer
	//    Public Property __invalid_name__24 As Integer
	//    Public Property SicBo_Combo4And6 As SicBoCombo4And6
	//    Public Property SicBo_Triple1 As SicBoTriple1
	//    Public Property SicBo_Combo3And5 As SicBoCombo3And5
	//    Public Property SicBo_Double4 As SicBoDouble4
	//    Public Property SicBo_Combo1And3 As SicBoCombo1And3
	//    Public Property SicBo_Combo3And6 As SicBoCombo3And6
	//    Public Property SicBo_Total11 As SicBoTotal11
	//    Public Property SicBo_2 As SicBo2
	//    Public Property SicBo_Total9 As SicBoTotal9
	//    Public Property SicBo_5 As SicBo5
	//    Public Property SicBo_4 As SicBo4
	//    Public Property __invalid_name__32 As Integer?
	//    Public Property __invalid_name__16 As Integer?
	//End Class

	//Public Class ASpot
	//    Public Property card As String
	//    Public Property score As Integer
	//End Class

	//Public Class BSpot
	//    Public Property card As String
	//    Public Property score As Integer
	//End Class

	//Public Class Banker
	//    Public Property score As Integer
	//    Public Property cards As List(Of String)
	//End Class

	//Public Class Player
	//    Public Property score As Integer
	//    Public Property cards As List(Of String)
	//End Class

	//Public Class Dragon
	//    Public Property card As String
	//    Public Property score As Integer
	//End Class

	//Public Class Tiger
	//    Public Property card As String
	//    Public Property score As Integer
	//End Class

	//Public Class DealerHand
	//    Public Property score As Integer
	//    Public Property cards As List(Of String)
	//End Class

	//Public Class WonSideBet
	//    Public Property code As String
	//    Public Property combination As String
	//End Class

	//Public Class Result
	//    Public Property outcomes As List(Of Object)
	//    Public Property luckyNumbers As LuckyNumbers
	//    Public Property aSpot As ASpot
	//    Public Property bSpot As BSpot
	//    Public Property outcome As String
	//    Public Property banker As Banker
	//    Public Property player As Player
	//    Public Property first As Integer?
	//    Public Property second As Integer?
	//    Public Property third As Integer?
	//    Public Property winningNumbers As Object
	//    Public Property dragon As Dragon
	//    Public Property tiger As Tiger
	//    Public Property dealtToPlayer As List(Of String)
	//    Public Property dealerHand As DealerHand
	//    Public Property wonSideBets As List(Of WonSideBet)
	//End Class

	public class Bet
	{
		public string code {get; set;}
		public double stake {get; set;}
		public double payout {get; set;}
		public DateTime placedOn {get; set;}
		public string description {get; set;}
		public string transactionId {get; set;}
	}

	//Public Class BetCoverage
	//    Public Property simple As Double
	//End Class

	//Public Class Hand1
	//    Public Property cards As List(Of String)
	//    Public Property decisions As List(Of Object)
	//    Public Property outcome As String
	//    Public Property score As Integer
	//End Class

	//Public Class Hands
	//    Public Property hand1 As Hand1
	//End Class

	public class Participant
	{
		public string userType {get; set;}
		public string country {get; set;}
		public string city {get; set;}
		public string channel {get; set;}
		public string device {get; set;}
		public string os {get; set;}
		public string screenName {get; set;}
		public string sessionId {get; set;}
		public List<Bet> bets {get; set;}
		public List<object> configOverlays {get; set;}
		public string casinoSessionId {get; set;}
		public object betCoverage {get; set;}
		public string subType {get; set;}
		public string playerId {get; set;}
		public string currency {get; set;}
		public string casinoId {get; set;}
		public string sideBetBankerPair {get; set;}
		public object hands {get; set;}
	}

	public class Table
	{
		public string id {get; set;}
		public string name {get; set;}
	}

	public class Game
	{
		public DateTime settledAt {get; set;}
		public string payout {get; set;}
		public object dealer {get; set;}
		public object result {get; set;}
		public List<Participant> participants {get; set;}
		public string id {get; set;}
		public string gameType {get; set;}
		public string status {get; set;}
		public DateTime startedAt {get; set;}
		public string currency {get; set;}
		public string wager {get; set;}
		public Table table {get; set;}
	}

	public class Datum
	{
		public string date {get; set;}
		public List<Game> games {get; set;}
	}

	public class CasinoStreamGamesDetailResponse
	{
		public string uuid {get; set;}
		public DateTime timestamp {get; set;}
		public List<Datum> data {get; set;}
	}


}
