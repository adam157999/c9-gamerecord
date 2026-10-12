using GameRecord.Gamess.VPOWER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static GameRecord.Gamess.FACHAI.FCAPI;

namespace GameRecord.Models
{
	public class PTSlotLobbyVM
	{
		public string playerName { get; set; }
		public string lang { get; set; }
		public List<PTSlotLobbyGame> gameList { get; set; }
	}

    public class PTV2LobbyVM
    {
        public string playerName { get; set; }
        public string lang { get; set; }
        public bool isLive { get; set; } = false;
        public List<PTV2SlotLobbyGame> gameList { get; set; }
    }

    public class RelaxLobbyVM
	{
		public string username { get; set; }
		public string lang { get; set; }
		public string currency { get; set; }
		public List<RelaxLobbyGame> gameList { get; set; }
	}

    public class ProviderLobbyVM
    {
        public string username { get; set; }
        public string lang { get; set; }
        public string currency { get; set; }
        public List<ProviderLobbyGame> gameList { get; set; }
    }

    public class HackSawLobbyVM
	{
		public string username { get; set; }
		public string lang { get; set; }
		public string currency { get; set; }
		public List<HackSawLobbyGame> gameList { get; set; }
	}

    public class Live22LobbyVM
	{
		public string Username { get; set; }
		public string Lang { get; set; }
		public string Currency { get; set; }
		public List<Live22LobbyVM_Game> Games { get; set; }
	}

	public class Live22LobbyVM_Game
	{
		public string GameCode { get; set; }
		public string GameName { get; set; }
		public string ImageUrl { get; set; }
	}

	public class GFGLobbyVM
	{
		public string username { get; set; }
		public string langid { get; set; }
		public string currency { get; set; }
		public List<GFGLobbyVM_Game> Games { get; set; }
	}

	public class GFGLobbyVM_Game
	{
		public int GameID { get; set; }
		public string GameName { get; set; }
		public string GameType { get; set; }
		public string ImageUrl { get; set; }
	}

	public class APLobbyVM
	{
		public string PlayerId { get; set; }
		public string LangCode { get; set; }
		public List<APLobbyVM_Game> Games { get; set; }
	}

	public class APLobbyVM_Game
	{
		public string GameCode { get; set; }
		public string GameName { get; set; }
		public string ImageUrl { get; set; }
	}

	public class DCTLobbyVM
	{
		public string PlayerId { get; set; }
		public string LangCode { get; set; }
		public List<DCTLobbyVM_Game> Games { get; set; }
	}

    public class DCTLobbyVM_Game
    {
        public int GameId { get; set; }
        public string GameName { get; set; }
        public string GameType { get; set; }
        public string ImageUrl { get; set; }
    }
    public class JDBFishLobbyVM
    {
        public dynamic gameList { get; set; }
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
    }

	public class FachaiFishLobbyVM
	{
		public dynamic gameList { get; set; }
		public string lang { get; set; }
		public string username { get; set; }
		public string currency { get; set; }
	}
	
	public class JiliFishLobbyVM
    {
        public List<JiLiFishLobbyGame> Games { get; set; }
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
    }

    public class CQ9FishLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<CQ9FishLobbyGame> games { get; set; }
    }
    public class VPowerFishLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<VPowerFishLobbyGame> games { get; set; }
    }

    public class VPowerFishLobbyGame
    {
        public string GameID { get; set; }
        public string GameName { get; set; }
        public string GameNameEN { get; set; }
        public string GameIcon { get; set; }
        public string Category { get; set; }
    }

    public class SpadeGamingFishLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<SpadeGamingFishLobbyGame> games { get; set; }
    }

    public class EvoGamingLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<EVORedTigerLobbyGame> games { get; set; }
    }

    public class EVONetEntGamingLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<EVONetEntLobbyGame> games { get; set; }
    }

    public class EVOBTGGamingLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<EVOBTGLobbyGame> games { get; set; }
    }

    public class EVONLCGamingLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<EVONLCLobbyGame> games { get; set; }
    }

    public class AWCFishLobbyVM
    {
        public List<JDBFishGame> gameList { get; set; }
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
    }

    public class ClotPlayLobbyVM
	{
		public string lang { get; set; }
		public string username { get; set; }
		public string currency { get; set; }
		public List<ClotPlayLobbyGame> games { get; set; }
	}

    public class OnlyPlayPlayLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<OnlyPlayLobbyGame> games { get; set; }
    }

    public class OctoPlayLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<OctoPlayLobbyGame> games { get; set; }
    }

    public class BeSoftPlayLobbyVM
    {
        public string lang { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public List<BeSoftLobbyGame> games { get; set; }
    }


    public class PTV2Login
    {
        public string PlayerId { get; set; }
        public string Password { get; set; }
        public string LangCode { get; set; }
        public string GameCode { get; set; }
        public string SystemId { get; set; }
        public string VirtualDatabase { get; set; }
        public string MobileHub { get; set; }
        public string Platform { get; set; }
        public string Lobby { get; set; } = "";
    }
}