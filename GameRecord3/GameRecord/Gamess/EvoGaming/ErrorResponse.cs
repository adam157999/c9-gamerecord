using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.EvoGaming
{
	[System.Xml.Serialization.XmlRoot("error")]
	public partial class ErrorResponse
	{
		public List<_Error> error { get; set; }
	}

	///<remarks/>
	[System.Xml.Serialization.XmlRoot("error")]
	public partial class _Error
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

	[System.Xml.Serialization.XmlRoot("error")]
	public partial class SingleErrorResponse
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