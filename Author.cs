using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1_Axis
{
    internal class Author
    {
		 
		private string voornaam;

		public string Voornaam
		{
			get 
			{ 
				return voornaam; 
			}
			set 
			{ 
				voornaam = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value); 
			}
		}
		private string achternaam;

		public string Achternaam
		{
			get 
			{ 
				return achternaam; 
			}
			set 
			{
                achternaam = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
            }
		}

		public string ToonNaam()
		{
			return $"{Voornaam} {Achternaam} ";
		}

	}
}
