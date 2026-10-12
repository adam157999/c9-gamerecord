using System;

namespace GameRecord.Gamess.EvoGaming
{
	[System.Xml.Serialization.XmlRoot("transfer")]
	public partial class TransferSuccessResponse
	{

		private string resultField;

		private decimal balanceField;

		private string etransidField;

		private string transidField;

		private string datetimeField;

		private string euidField;

		private string uidField;

		//20230404
		private string errormsgField;

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
		public decimal balance
		{
			get
			{
				return this.balanceField;
			}
			set
			{
				this.balanceField = value;
			}
		}

		///<remarks/>
		public string etransid
		{
			get
			{
				return this.etransidField;
			}
			set
			{
				this.etransidField = value;
			}
		}

		///<remarks/>
		public string transid
		{
			get
			{
				return this.transidField;
			}
			set
			{
				this.transidField = value;
			}
		}

		///<remarks/>
		public string datetime
		{
			get
			{
				return this.datetimeField;
			}
			set
			{
				this.datetimeField = value;
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

		//20230404
		public string errormsg
		{
			get
			{
				return this.errormsgField;
			}
			set
			{
				this.errormsgField = value;
			}
		}
	}

	///<remarks/>
	[System.Xml.Serialization.XmlRoot("transfer")]
	public partial class TransferFailedResponse
	{

		private string resultField;

		private string errormsgField;

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
		public string errormsg
		{
			get
			{
				return this.errormsgField;
			}
			set
			{
				this.errormsgField = value;
			}
		}
	}
}