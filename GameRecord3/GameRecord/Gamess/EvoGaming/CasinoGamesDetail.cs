using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming.CasinoGamesDetail
{

	public class CasinoGamesDetailResponse
	{
		public string uuid {get; set;}
		public DateTime timestamp {get; set;}
		public List<Datum> data {get; set;}
	}

	public class Datum
	{
		public string _date {get; set;}
		public List<Game> games {get; set;}
	}

	public class Game
	{
		public DateTime settledAt {get; set;}
		public decimal? payout {get; set;}
		public Dealer dealer {get; set;}
		public Result result {get; set;}
		public List<Participant> participants {get; set;}
		public string id {get; set;}
		public string gameType {get; set;}
		public string status {get; set;}
		public DateTime startedAt {get; set;}
		public string currency {get; set;}
		public decimal? wager {get; set;}
		public Table table {get; set;}
	}

	public class Dealer
	{
		public string uid {get; set;}
		public string name {get; set;}
	}

	public class Result
	{
		public Banker banker {get; set;}
		public Player player {get; set;}
		public string outcome {get; set;}
		public Redenvelopepayouts redEnvelopePayouts {get; set;}
	}

	public class Banker
	{
		public int score {get; set;}
		public List<string> cards {get; set;}
	}

	public class Player
	{
		public int score {get; set;}
		public List<string> cards {get; set;}
	}

	public class Redenvelopepayouts
	{
		public int BAC_BankerPair {get; set;}
		public int BAC_Tie {get; set;}
		public int BAC_PlayerPair {get; set;}
	}

	public class Table
	{
		public string id {get; set;}
		public string name {get; set;}
	}

	public class Participant
	{
		public string playerId {get; set;}
		public string screenName {get; set;}
		public string playerGameId {get; set;}
		public string sessionId {get; set;}
		public string casinoSessionId {get; set;}
		public string currency {get; set;}
		public List<Bet> bets {get; set;}
		public List<string> configOverlays {get; set;}
		public string subType {get; set;}
		public string playMode {get; set;}
		public string sideBetPlayerPair {get; set;}
		public string casinoId {get; set;}
		public string sideBetBankerPair {get; set;}
	}

	public class Bet
	{
		public string code {get; set;}
		public int stake {get; set;}
		public int payout {get; set;}
		public DateTime placedOn {get; set;}
		public string description {get; set;}
		public string transactionId {get; set;}
	}

}

//Namespace CasinoGamesDetail
//    Public Class CasinoGamesDetailResponse
//        Public Property data As List(Of Datum)
//        Public Property timestamp As String
//        Public Property uuid As String
//    End Class

//    Public Class Datum
//        Public Property _date As String
//        Public Property games As List(Of Game)
//    End Class

//    Public Class Game
//        Public Property currency As String
//        Public Property dealer As Dealer
//        Public Property id As String
//        Public Property participants As Participant
//        Public Property payout As Decimal
//        Public Property result As Result
//        Public Property settledAt As String
//        Public Property startedAt As String
//        Public Property status As String
//        Public Property table As Table
//        Public Property gameType As String
//        Public Property wager As Decimal
//    End Class

//    Public Class Dealer
//        Public Property name As String
//        Public Property uid As String
//    End Class

//    Public Class Result
//        Public Property banker As Banker
//        Public Property player As Player
//        Public Property outcome As String
//    End Class

//    Public Class Banker
//        Public Property score As Decimal
//        Public Property cards As String()
//    End Class

//    Public Class Player
//        Public Property score As Decimal
//        Public Property cards As String()
//    End Class
//    Public Class Table
//        Public Property id As String
//        Public Property name As String
//    End Class

//    Public Class Participant
//        Public Property bets As List(Of Bet)
//        Public Property casinoId As String
//        Public Property configOverlays As List(Of String)
//        Public Property currency As String
//        Public Property betCoverage As List(Of Betcoverage)
//        Public Property playerId As String
//        Public Property screenName As String
//        Public Property sessionId As String
//        Public Property casinoSessionId As String
//        Public Property sideBetBankerBonus As String
//        Public Property sideBetBankerPair As String
//        Public Property sideBetEitherPair As String
//        Public Property sideBetPerfectPair As String
//        Public Property sideBetPlayerBonus As String
//        Public Property sideBetPlayerPair As String
//        Public Property sideBetSuperSix As String
//        Public Property suitedTie As String
//        Public Property seats As Seats
//        Public Property sideBetAABonus As Sidebetaabonus
//        Public Property sideBet5p1 As Sidebet5p1
//        Public Property sideBetPairPlus As Sidebetpairplus
//        Public Property sideBet6CardBonus As Sidebet6cardbonus
//        Public Property sideBetPairOrBetter As Sidebetpairorbetter
//        Public Property sideBetTrips As Sidebettrips
//        Public Property sideBetBestFive As Sidebetbestfive
//        Public Property decisions As List(Of Decision)
//    End Class

//    Public Class Betcoverage
//    End Class

//    Public Class Seats
//        Public Property Seat_number As Seat_Number
//    End Class

//    Public Class Seat_Number
//        Public Property betBehind As Boolean
//        Public Property doubleDown As Boolean
//        Public Property insurance As Boolean
//        Public Property sideBetPerfectPair As Sidebetperfectpair
//        Public Property splitHand As Boolean
//    End Class

//    Public Class Sidebetperfectpair
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebetaabonus
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebet5p1
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebetpairplus
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebet6cardbonus
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebetpairorbetter
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebettrips
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Sidebetbestfive
//        Public Property betCode As String
//        Public Property result As String
//    End Class

//    Public Class Bet
//        Public Property code As String
//        Public Property description As String
//        Public Property payout As Decimal
//        Public Property placedOn As String
//        Public Property stake As Decimal
//        Public Property transactionId As String
//    End Class

//    Public Class Decision
//        Public Property type As String
//    End Class
//End Namespace