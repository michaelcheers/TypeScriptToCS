using System;
using System.Collections;
using System.Collections.Generic;

namespace TypeScriptToCS
{
	public class SkippedWord
	{
		public string Word;
		public Dictionary<string, string> Wheres;
		public SkippedWord()
		{
			Word = String.Empty;
			Wheres = new Dictionary<string, string>();
		}
	}
}

