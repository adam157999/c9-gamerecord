using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Data.SqlClient;
using System.Data;
using System.Net;

namespace GameRecord
{
	public class CommonUAT
	{
		public string BingoStagingConn1 = System.Configuration.ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ToString();
		private static readonly Random random = new Random();

		//public void LogError(string sMsg, string sGame)
		//{
		//    string path = HttpContext.Current.Server.MapPath(@"~\Logs\").ToString();
		//    DateTime dtNow = DateTime.Now;
		//    string date = string.Format("{0:yyyyMMdd}", dtNow);
		//    string time = string.Format("{0:HH:mm:ss.fff}", dtNow);
		//    path = path + sGame + "_Log_" + date + ".txt";
		//    if (!File.Exists(path))
		//    {
		//        using (StreamWriter log = new StreamWriter(path))
		//        {
		//            log.WriteLine(time);
		//            log.WriteLine(sMsg);
		//            log.WriteLine("");
		//        }
		//    }
		//    else
		//    {
		//        using (StreamWriter log = File.AppendText(path))
		//        {
		//            log.WriteLine(time);
		//            log.WriteLine(sMsg);
		//            log.WriteLine("");
		//        }
		//    }
		//}

		public void LogError(string sMsg, string sGame)
		{
			DateTime dtNow = DateTime.Now;
			string date = string.Format("{0:yyyyMMdd}", dtNow);
			string time = string.Format("{0:HH:mm:ss.fff}", dtNow);
			string fileName = sGame + "_Log_" + date + ".txt";

			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

			CloudStorageAccount storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureStorageAccount_ConnectionString"]);
			CloudBlobClient client = storage.CreateCloudBlobClient();
			CloudBlobContainer container = client.GetContainerReference("api-gamerec-logs");
			CloudAppendBlob blob = container.GetAppendBlobReference(fileName);

			if (!blob.Exists()) blob.CreateOrReplace();
			using (MemoryStream stream = new MemoryStream())
			{
				using (StreamWriter sw = new StreamWriter(stream))
				{
					sw.Write(time + "\n" + sMsg + "\n\n");
					sw.Flush();
					stream.Position = 0;
					blob.AppendFromStream(stream);
				}
			}
		}

		public bool VerifyEmail(string email)
		{
			try
			{
				MailAddress mail = new MailAddress(email);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		public string FormatPhoneNumberMY(string sPhone)
		{
			if (sPhone == null) sPhone = "";
			sPhone = sPhone.Replace("-", "");
			sPhone = sPhone.Replace("+", "");
			sPhone = sPhone.Replace(" ", "");
			if (!(sPhone.All(char.IsDigit) && sPhone.Length >= 9 && sPhone.Length <= 13))
			{
				return "";
			}
			if (sPhone.Substring(0, 2) == "01")
				sPhone = "601" + sPhone.Substring(2, sPhone.Length - 2);
			if (sPhone.Substring(0, 3) != "601")
				return "";
			return sPhone;
		}

		public string FormatPhoneNumberSG(string sPhone)
		{
			if (sPhone == null) sPhone = "";
			sPhone = sPhone.Replace("-", "");
			sPhone = sPhone.Replace("+", "");
			sPhone = sPhone.Replace(" ", "");
			if (!(sPhone.All(char.IsDigit) && sPhone.Length >= 9 && sPhone.Length <= 11))
			{
				return "";
			}
			if (sPhone.Substring(0, 2) != "65" && sPhone.Substring(0, 3) != "602")
				return "";
			return sPhone;
		}

		public string FormatIC(string sSource)
		{
			string sResult = "";
			sResult = sSource.Replace("-", "").Trim();
			if (sResult.Trim().Length == 12)
				return sResult;
			else
				return "";
		}

		public string SendSmsToEsms(string sNumber, string sMessage)
		{
			if (ConfigurationManager.AppSettings["ESMS"] != "on") return "OFF";

			var url = "https://api.esms.com.my/sms/send";
			try
			{
				var values = new Dictionary<string, string>
								{
									 { "user", ConfigurationManager.AppSettings["ESMSUsername"] },
									 { "pass", ConfigurationManager.AppSettings["ESMSPassword"] },
                   //{ "to", "60165128571" },
                   { "to", sNumber },
									 { "msg", "RM0.00 " + ConfigurationManager.AppSettings["ESMSFrom"] + ": " + sMessage }
								};

				var content = new FormUrlEncodedContent(values);
				var client = new HttpClient();
				var response = client.PostAsync(url, content);

				//var responseString = await response.Content.ReadAsStringAsync();
				//Console.WriteLine("Response: " + responseString);

				return "OK";
			}
			catch (Exception ex)
			{
				LogError("Error:\n" + ex.Message.ToString(), "SMS");
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);

				return "NOT OK";
			}
		}

		public async Task<double> CheckBalanceEsmsAsync()
		{
			var url = "https://api.esms.com.my/sms/balance";
			try
			{
				var values = new Dictionary<string, string>
								{
									 { "user", ConfigurationManager.AppSettings["ESMSUsername"] },
									 { "pass", ConfigurationManager.AppSettings["ESMSPassword"] }
								};

				var content = new FormUrlEncodedContent(values);
				var client = new HttpClient();
				var response = await client.PostAsync(url, content);

				var responseString = await response.Content.ReadAsStringAsync();
				//Console.WriteLine("Response: " + responseString);

				EsmsCheckBalance result = new EsmsCheckBalance();
				result = JsonConvert.DeserializeObject<EsmsCheckBalance>(responseString);

				return result.balance / 1000;
			}
			catch (Exception ex)
			{
				LogError("Error:\n" + ex.Message.ToString(), "Er");
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				return 0;
			}
		}

		public string GenerateOtp()
		{
			//alphanumeric
			//string code = Membership.GeneratePassword(6, 0);
			//code = Regex.Replace(code, @"[^a-zA-Z0-9]", m => "9");
			//return code;

			//number only
			string code = "";
			lock (random)
				code = random.Next(1, 100000).ToString();
			while (code.Length < 6)
				code = "0" + code;
			return code;
		}

		public string MD5Hash(string input)
		{
			StringBuilder hash = new StringBuilder();
			MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
			byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

			for (int i = 0; i < bytes.Length; i++)
			{
				hash.Append(bytes[i].ToString("x2"));
			}
			return hash.ToString();
		}

		public string OpenSSLEncrypt(string plainText, string passphrase)
		{
			// generate salt
			byte[] key, iv;
			byte[] salt = new byte[8];
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			rng.GetNonZeroBytes(salt);
			DeriveKeyAndIV(passphrase, salt, out key, out iv);
			// encrypt bytes
			byte[] encryptedBytes = EncryptStringToBytesAes(plainText, key, iv);
			// add salt as first 8 bytes
			byte[] encryptedBytesWithSalt = new byte[salt.Length + encryptedBytes.Length + 8];
			Buffer.BlockCopy(Encoding.ASCII.GetBytes("Salted__"), 0, encryptedBytesWithSalt, 0, 8);
			Buffer.BlockCopy(salt, 0, encryptedBytesWithSalt, 8, salt.Length);
			Buffer.BlockCopy(encryptedBytes, 0, encryptedBytesWithSalt, salt.Length + 8, encryptedBytes.Length);
			// base64 encode
			return Convert.ToBase64String(encryptedBytesWithSalt);
		}

		public string OpenSSLDecrypt(string encrypted, string passphrase)
		{
			// base 64 decode
			byte[] encryptedBytesWithSalt = Convert.FromBase64String(encrypted);
			// extract salt (first 8 bytes of encrypted)
			byte[] salt = new byte[8];
			byte[] encryptedBytes = new byte[encryptedBytesWithSalt.Length - salt.Length - 8];
			Buffer.BlockCopy(encryptedBytesWithSalt, 8, salt, 0, salt.Length);
			Buffer.BlockCopy(encryptedBytesWithSalt, salt.Length + 8, encryptedBytes, 0, encryptedBytes.Length);
			// get key and iv
			byte[] key, iv;
			DeriveKeyAndIV(passphrase, salt, out key, out iv);
			return DecryptStringFromBytesAes(encryptedBytes, key, iv);
		}

		private static void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
		{
			// generate key and iv
			List<byte> concatenatedHashes = new List<byte>(48);

			byte[] password = Encoding.UTF8.GetBytes(passphrase);
			byte[] currentHash = new byte[0];
			MD5 md5 = MD5.Create();
			bool enoughBytesForKey = false;
			// See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
			while (!enoughBytesForKey)
			{
				int preHashLength = currentHash.Length + password.Length + salt.Length;
				byte[] preHash = new byte[preHashLength];

				Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
				Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
				Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

				currentHash = md5.ComputeHash(preHash);
				concatenatedHashes.AddRange(currentHash);

				if (concatenatedHashes.Count >= 48)
					enoughBytesForKey = true;
			}

			key = new byte[32];
			iv = new byte[16];
			concatenatedHashes.CopyTo(0, key, 0, 32);
			concatenatedHashes.CopyTo(32, iv, 0, 16);

			md5.Clear();
			md5 = null;
		}

		static byte[] EncryptStringToBytesAes(string plainText, byte[] key, byte[] iv)
		{
			// Check arguments.
			if (plainText == null || plainText.Length <= 0)
				throw new ArgumentNullException("plainText");
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException("key");
			if (iv == null || iv.Length <= 0)
				throw new ArgumentNullException("iv");

			// Declare the stream used to encrypt to an in memory
			// array of bytes.
			MemoryStream msEncrypt;

			// Declare the RijndaelManaged object
			// used to encrypt the data.
			RijndaelManaged aesAlg = null;

			try
			{
				// Create a RijndaelManaged object
				// with the specified key and IV.
				aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				msEncrypt = new MemoryStream();
				using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
					{

						//Write all data to the stream.
						swEncrypt.Write(plainText);
						swEncrypt.Flush();
						swEncrypt.Close();
					}
				}
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			// Return the encrypted bytes from the memory stream.
			return msEncrypt.ToArray();
		}

		static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
		{
			// Check arguments.
			if (cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException("cipherText");
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException("key");
			if (iv == null || iv.Length <= 0)
				throw new ArgumentNullException("iv");

			// Declare the RijndaelManaged object
			// used to decrypt the data.
			RijndaelManaged aesAlg = null;

			// Declare the string used to hold
			// the decrypted text.
			string plaintext;

			try
			{
				// Create a RijndaelManaged object
				// with the specified key and IV.
				aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

				// Create a decrytor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
				// Create the streams used for decryption.
				using (MemoryStream msDecrypt = new MemoryStream(cipherText))
				{
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader srDecrypt = new StreamReader(csDecrypt))
						{
							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
							srDecrypt.Close();
						}
					}
				}
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			return plaintext;
		}

		public bool InsertLog(string sType, string sField1 = "", object sValue1 = null, string sField2 = "", object sValue2 = null, string sField3 = "", object sValue3 = null, string sField4 = "", object sValue4 = null, string sField5 = "", object sValue5 = null,
									string sField6 = "", object sValue6 = null, string sField7 = "", object sValue7 = null, string sField8 = "", object sValue8 = null, string sField9 = "", object sValue9 = null, string sField10 = "", object sValue10 = null,
									string sField11 = "", object sValue11 = null, string sField12 = "", object sValue12 = null, string sField13 = "", object sValue13 = null)
		{
			try
			{
				SqlConnection scon = new SqlConnection(BingoStagingConn1);
				string sID = Guid.NewGuid().ToString();
				DateTime dtNow = DateTime.Now;

				string query = "INSERT INTO dbo.Tracker(Id, Type, Field1, Value1, Field2, Value2, Field3, Value3, Field4, Value4, Field5, Value5, Field6, Value6, Field7, Value7, Field8, Value8, Field9, Value9, Field10, Value10, Field11, Value11, Field12, Value12, Field13, Value13, CreatedDate, Deleted) VALUES ";
				query += "(@id,@type, @Field1, @Value1, @Field2, @Value2, @Field3, @Value3, @Field4, @Value4, @Field5, @Value5, @Field6, @Value6, @Field7, @Value7, @Field8, @Value8, @Field9, @Value9, @Field10, @Value10, @Field11, @Value11, @Field12, @Value12, @Field13, @Value13, @CreatedDate, @del)";
				SqlCommand cmd = new SqlCommand(query, scon);
				scon.Open();
				cmd.Parameters.AddWithValue("@id", sID);
				cmd.Parameters.AddWithValue("@type", sType);
				cmd.Parameters.AddWithValue("@Field1", sField1);
				cmd.Parameters.AddWithValue("@Value1", sValue1 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field2", sField2);
				cmd.Parameters.AddWithValue("@Value2", sValue2 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field3", sField3);
				cmd.Parameters.AddWithValue("@Value3", sValue3 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field4", sField4);
				cmd.Parameters.AddWithValue("@Value4", sValue4 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field5", sField5);
				cmd.Parameters.AddWithValue("@Value5", sValue5 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field6", sField6);
				cmd.Parameters.AddWithValue("@Value6", sValue6 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field7", sField7);
				cmd.Parameters.AddWithValue("@Value7", sValue7 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field8", sField8);
				cmd.Parameters.AddWithValue("@Value8", sValue8 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field9", sField9);
				cmd.Parameters.AddWithValue("@Value9", sValue9 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field10", sField10);
				cmd.Parameters.AddWithValue("@Value10", sValue10 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field11", sField11);
				cmd.Parameters.AddWithValue("@Value11", sValue11 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field12", sField12);
				cmd.Parameters.AddWithValue("@Value12", sValue12 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Field13", sField13);
				cmd.Parameters.AddWithValue("@Value13", sValue13 ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@CreatedDate", dtNow);
				cmd.Parameters.AddWithValue("@del", '0');
				cmd.ExecuteNonQuery();
				scon.Close();

				return true;
			}
			catch (Exception ex)
			{
				//return ex.Message.ToString();
				return false;
			}
		}

		public class EsmsCheckBalance
		{
			public int status { get; set; }
			public int balance { get; set; }
			public string message { get; set; }
			public string id { get; set; }
		}

		public string SendSmsToBulkSms(string Phone, string Message)
		{
			try
			{
				if (ConfigurationManager.AppSettings["BulkSms"] != "on") return "OFF";

				string Username = ConfigurationManager.AppSettings["BulkSmsUsername"];
				string Password = ConfigurationManager.AppSettings["BulkSmsPassword"];
				//string sUrl = "http://58.84.42.79/bulksms/gateway/httprecv.php";
				string sUrl = ConfigurationManager.AppSettings["SMSDOmain"];

				string sParameters = "username=" + Username + "&password=" + Password + "&to=" + Phone + "&text=" + Message;
				using (WebClient wc = new WebClient())
				{
					wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
					string HtmlResult = wc.UploadString(sUrl, sParameters);
				}

				return "OK";
			}
			catch (Exception ex)
			{
				return "NOT OK";
			}
		}
	}
}