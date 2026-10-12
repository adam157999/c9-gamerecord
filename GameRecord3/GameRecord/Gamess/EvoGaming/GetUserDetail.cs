using System;

namespace GameRecord.Gamess.EvoGaming
{
	[System.Xml.Serialization.XmlRoot("userdetails")]
	public partial class GetUserDetailSuccessResponse
	{

		private string emailaddressField;

		private string firstnameField;

		private string lastnameField;

		private string screennameField;

		private string countrycodeField;

		private string uidField;

		private string euidField;

		//20230404
		private string errormsgField;

		///<remarks/>
		public string emailaddress
		{
			get
			{
				return this.emailaddressField;
			}
			set
			{
				this.emailaddressField = value;
			}
		}

		///<remarks/>
		public string firstname
		{
			get
			{
				return this.firstnameField;
			}
			set
			{
				this.firstnameField = value;
			}
		}

		///<remarks/>
		public string lastname
		{
			get
			{
				return this.lastnameField;
			}
			set
			{
				this.lastnameField = value;
			}
		}

		///<remarks/>
		public string screenname
		{
			get
			{
				return this.screennameField;
			}
			set
			{
				this.screennameField = value;
			}
		}

		///<remarks/>
		public string countrycode
		{
			get
			{
				return this.countrycodeField;
			}
			set
			{
				this.countrycodeField = value;
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

}
