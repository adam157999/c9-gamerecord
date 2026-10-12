using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameRecord.Controllers.SeamlessWallet.Model
{
	public class PragmaticBalanceRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string token { get; set; }
	}

	public class PragmaticBalanceResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal cash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal bonus { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}

	public class PragmaticVerifySessionRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string token { get; set; }
	}

	public class PragmaticVerifySessionResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal cash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal bonus { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}

	public class PragmaticCreditRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string gameId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string roundId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal amount { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string reference { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public long timestamp { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string roundDetails { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string jackpotId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string jackpotDetails { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string campaignId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string campaignType { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string bonusCode { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal promoWinAmount { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string promoWinReference { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string promoCampaignID { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string promoCampaignType { get; set; }
	}

	public class PragmaticCreditResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string transactionId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal cash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal bonus { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}

	public class PragmaticDebitRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string gameId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string roundId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal amount { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string reference { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public long timestamp { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string roundDetails { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string bonusCode { get; set; }

	}


	public class PragmaticDebitResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string transactionId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal cash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal bonus { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal usedPromo { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}

	public class PragmaticRollbackRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string reference { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string gameId { get; set; }
	}

	public class PragmaticRollbackResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string transactionId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}

	public class PragmaticAdjustmentRequest
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string hash { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string userId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string gameId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string roundId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal amount { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string reference { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string providerId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public long timestamp { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string token { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal validBetAmount { get; set; }

	}


	public class PragmaticAdjustmentResponse
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string transactionId { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string currency { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal? cash { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal? bonus { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string error { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string description { get; set; }
	}
}