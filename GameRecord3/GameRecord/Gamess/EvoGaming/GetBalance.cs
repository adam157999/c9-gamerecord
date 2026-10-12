using System;

namespace GameRecord.Gamess.EvoGaming
{
	///<remarks/>
	[System.Xml.Serialization.XmlRoot("userbalance")]
	public partial class GetBalanceSuccessResponse
	{

		private string resultField;

		private string euidField;

		private string uidField;

		private decimal tbalanceField;

		private decimal abalanceField;

		///<remarks/>
		public string result
		{
			get
			{
				return this.resultField;
			}
			set
			{
				this.resultField = value;
			}
		}

		///<remarks/>
		public string euid
		{
			get
			{
				return this.euidField;
			}
			set
			{
				this.euidField = value;
			}
		}

		///<remarks/>
		public string uid
		{
			get
			{
				return this.uidField;
			}
			set
			{
				this.uidField = value;
			}
		}

		///<remarks/>
		public decimal tbalance
		{
			get
			{
				return this.tbalanceField;
			}
			set
			{
				this.tbalanceField = value;
			}
		}

		///<remarks/>
		public decimal abalance
		{
			get
			{
				return this.abalanceField;
			}
			set
			{
				this.abalanceField = value;
			}
		}
	}

}