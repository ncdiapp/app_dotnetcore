using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeBL.Extension
{
    internal static class ExceptionExtension
	{
		public static string GetInnerMessage(this Exception ex)
		{
			while (ex.InnerException != null && string.IsNullOrWhiteSpace(ex.InnerException.Message) == false)
			{
				ex = ex.InnerException;
			}

			return ex.Message.Trim().TrimEnd(System.Environment.NewLine.ToCharArray());
		}
	}
}
