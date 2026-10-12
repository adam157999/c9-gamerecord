using System;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming
{
	public class UserAuthRequest
	{
		public string uuid { get; set; }
		public Player player { get; set; }
		public Config config { get; set; }

		public UserAuthRequest()
		{
			player = new Player { session = new Session(), group = new Group() };
			config = new Config { brand = new Brand(), channel = new Channel(), game = new Game { table = new Table() }, urls = new Urls() };
		}

	}

	public class Player
	{
		public string id { get; set; }
		public bool update { get; set; }
		public string firstName { get; set; }
		public string lastName { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string nickname { get; set; }
		public string country { get; set; }
		public string language { get; set; }
		public string currency { get; set; }
		public Session session { get; set; }
		public Group group { get; set; }
	}

	public class Session
	{
		public string id { get; set; }
		public string ip { get; set; }
	}

	public class Group
	{
		public string id { get; set; }
		public string action { get; set; }
	}

	public class Config
	{
		public Brand brand { get; set; }
		public Game game { get; set; }
		public Channel channel { get; set; }
		public Urls urls { get; set; }
	}

	public class Brand
	{
		public string id { get; set; }
		public string skin { get; set; }
	}

	public class Game
	{
		[DefaultValue(Constant.GameCategory.Roulette), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string category { get; set; }

		[JsonProperty("interface", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string _interface { get; set; }

		public Table table { get; set; }
	}

	public class Table
	{
		public string id { get; set; }
	}

	public class Channel
	{
		public bool wrapped { get; set; }
		public bool mobile { get; set; }
	}

	public class Urls
	{
		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string cashier { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string responsibleGaming { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string lobby { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string sessionTimeout { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string gameHistory { get; set; }

		[DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public string realityCheckURL { get; set; }
	}


	public class UserAuthSuccessResponse
	{
		public string entry { get; set; }
		public string entryEmbedded { get; set; }
	}


	public class UserAuthFailedResponse
	{
		public List<Errors> errors { get; set; }
	}

	public class Errors
	{
		public string code { get; set; }
		public string message { get; set; }
	}
}

