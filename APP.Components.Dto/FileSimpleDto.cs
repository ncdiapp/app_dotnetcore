using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{

	public class FileSimpleDto
	{


		public static readonly Dictionary<string, FileSimpleDto> DictExternalByteFileGuIdFileDto = new Dictionary<string, FileSimpleDto>();


		public List<byte> OriginalImage
		{
			get; set;

		}

		public string FileName
		{
			get; set;

		}

		public string ExternalByteFileGuId
		{
			get; set;

		}

		public string ContenType
		{
			get; set;

		}
	}

}
