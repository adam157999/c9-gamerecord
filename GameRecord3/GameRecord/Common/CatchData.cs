using GameRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace GameRecord
{
    public static class CatchData
    {
        private static List<GameApiKey> _gameApiKeys;

        public static void LoadApiKeys()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ToString()))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("sp_GameApiKey_Sel", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Type", "0");
                cmd.Parameters.AddWithValue("@ApiType", "GM");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable tbl = new DataTable();
                da.Fill(tbl);

                if (tbl.Rows.Count > 0)
                {
                    _gameApiKeys = tbl.AsEnumerable().Select(x => new GameApiKey()
                    {
                        GameID = x.Field<string>("GameID"),
                        CurrencyID = x.Field<string>("CurrencyID"),
                        Environment = x.Field<string>("Environment"),
                        ApiConfig = x.Field<string>("ApiConfig"),
                    }).ToList();
                }
            }
        }

        public static List<GameApiKey> GetApiKeysByProvider(string gameID)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.GameID == gameID).ToList();
        }

        public static List<GameApiKey> GetApiKeysByEnv(string env)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.Environment == env).ToList();
        }

        public static List<GameApiKey> GetApiKeysByCurrency(string currencyID)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.CurrencyID == currencyID).ToList();
        }

        public static List<GameApiKey> GetApiKeys(string gameID)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.GameID == gameID).ToList();
        }

        public static GameApiKey GetApiKeys(string gameID, string env)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.GameID == gameID && x.Environment == env).FirstOrDefault();
        }

        public static GameApiKey GetApiKeys(string gameID, string env, string currencyID)
        {
            if (_gameApiKeys == null) LoadApiKeys();
            return _gameApiKeys.Where(x => x.GameID == gameID && x.Environment == env && x.CurrencyID == currencyID).FirstOrDefault();
        }
    }
}