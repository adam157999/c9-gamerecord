using System;

//<System.Xml.Serialization.XmlRoot("transaction")> _
//Partial Public Class GetTransactionSuccessResponse
//    Public Property transaction As List(Of GetTransactionSuccess)
//End Class

namespace GameRecord.Gamess.EvoGaming
{
	[System.Xml.Serialization.XmlRoot("transaction")]
	public partial class GetTransactionSuccessResponse
	{

		private string resultField;

		private decimal balanceField;

		private string etransidField;

		private string transidField;

		private string datetimeField;

		private DateTime datetimeisoField;

		private string uIDField;

		private string euIDField;

		private string cCodeField;

		private decimal amountField;

		private string[] textField;

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
		[System.Xml.Serialization.XmlElementAttribute("datetime-iso")]
		public DateTime datetimeiso
		{
			get
			{
				return this.datetimeisoField;
			}
			set
			{
				this.datetimeisoField = value;
			}
		}

		///<remarks/>
		public string uID
		{
			get
			{
				return this.uIDField;
			}
			set
			{
				this.uIDField = value;
			}
		}

		///<remarks/>
		public string euID
		{
			get
			{
				return this.euIDField;
			}
			set
			{
				this.euIDField = value;
			}
		}

		///<remarks/>
		public string cCode
		{
			get
			{
				return this.cCodeField;
			}
			set
			{
				this.cCodeField = value;
			}
		}

		///<remarks/>
		public decimal amount
		{
			get
			{
				return this.amountField;
			}
			set
			{
				this.amountField = value;
			}
		}

		///<remarks/>
		[System.Xml.Serialization.XmlTextAttribute()]
		public string[] Text
		{
			get
			{
				return this.textField;
			}
			set
			{
				this.textField = value;
			}
		}
	}
}