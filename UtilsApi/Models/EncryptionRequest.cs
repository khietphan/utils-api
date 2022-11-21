using System;
namespace UtilsApi.Models
{
	public class EncryptionRequest
	{
		public string Key { get; set; }

        public string PlainText { get; set; }
    }
}

