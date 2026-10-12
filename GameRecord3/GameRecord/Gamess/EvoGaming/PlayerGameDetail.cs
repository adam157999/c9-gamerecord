using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming.PlayerGameDetail
{
	public class PlayerGameDetailResponse
	{
		public List<Datum> data {get; set;}
		public string timestamp {get; set;}
		public string uuid {get; set;}
	}

	public class Datum
	{
		public Aams aams {get; set;}
		public Dealer dealer {get; set;}
		public string id {get; set;}
		public List<Participant> participants {get; set;}
		public Result result {get; set;}
		public string settledAt {get; set;}
		public string startedAt {get; set;}
		public string status {get; set;}
		public Table table {get; set;}
		public string gameType {get; set;}
	}

	public class Aams
	{
		public string gameSessionId {get; set;}
		public string participationId {get; set;}
	}

	public class Dealer
	{
		public string name {get; set;}
		public string uid {get; set;}
	}

	public class Result
	{
	}

	public class Table
	{
		public string id {get; set;}
		public string name {get; set;}
	}

	public class Participant
	{
		public List<Bet> bets {get; set;}
		public string casinoId {get; set;}
		public List<string> configOverlays {get; set;}
		public string currency {get; set;}
		public Betcoverage betCoverage {get; set;}
		public string playerId {get; set;}
		public string screenName {get; set;}
		public string sessionId {get; set;}
		public string casinoSessionId {get; set;}
		public string sideBetBankerBonus {get; set;}
		public string sideBetBankerPair {get; set;}
		public string sideBetEitherPair {get; set;}
		public string sideBetPerfectPair {get; set;}
		public string sideBetPlayerBonus {get; set;}
		public string sideBetSuperSix {get; set;}
		public string suitedTie {get; set;}
		public Seats seats {get; set;}
		public Sidebetaabonus sideBetAABonus {get; set;}
		public Sidebet5p1 sideBet5p1 {get; set;}
		public Sidebetpairplus sideBetPairPlus {get; set;}
		public Sidebet6cardbonus sideBet6CardBonus {get; set;}
		public Sidebetpairorbetter sideBetPairOrBetter {get; set;}
		public Sidebettrips sideBetTrips {get; set;}
		public Sidebetbestfive sideBetBestFive {get; set;}
		public List<Decision> decisions {get; set;}
	}

	public class Betcoverage
	{
	}

	public class Seats
	{
		public Seat_Number Seat_number {get; set;}
	}

	public class Seat_Number
	{
		public bool betBehind {get; set;}
		public bool doubleDown {get; set;}
		public bool insurance {get; set;}
		public Sidebetperfectpair sideBetPerfectPair {get; set;}
		public bool splitHand {get; set;}
	}

	public class Sidebetperfectpair
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebetaabonus
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebet5p1
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebetpairplus
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebet6cardbonus
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebetpairorbetter
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebettrips
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Sidebetbestfive
	{
		public string betCode {get; set;}
		public string result {get; set;}
	}

	public class Bet
	{
		public string code {get; set;}
		public string description {get; set;}
		public int payout {get; set;}
		public string placedOn {get; set;}
		public int stake {get; set;}
		public string transactionId {get; set;}
	}

	public class Decision
	{
		public string type {get; set;}
	}
}