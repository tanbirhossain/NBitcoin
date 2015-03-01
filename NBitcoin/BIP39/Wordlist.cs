﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Wordlist
	{
		static Wordlist()
		{
			WordlistSource = new HardcodedWordlistSource();
		}
		private static Wordlist _Japanese;
		public static Wordlist Japanese
		{
			get
			{
				if(_Japanese == null)
					_Japanese = LoadWordList(Language.Japanese).Result;
				return _Japanese;
			}
		}

		private static Wordlist _ChineseSimplified;
		public static Wordlist ChineseSimplified
		{
			get
			{
				if(_ChineseSimplified == null)
					_ChineseSimplified = LoadWordList(Language.ChineseSimplified).Result;
				return _ChineseSimplified;
			}
		}

		private static Wordlist _ChineseTraditional;
		public static Wordlist ChineseTraditional
		{
			get
			{
				if(_ChineseTraditional == null)
					_ChineseTraditional = LoadWordList(Language.ChineseTraditional).Result;
				return _ChineseTraditional;
			}
		}

		private static Wordlist _Spanish;
		public static Wordlist Spanish
		{
			get
			{
				if(_Spanish == null)
					_Spanish = LoadWordList(Language.Spanish).Result;
				return _Spanish;
			}
		}

		private static Wordlist _English;
		public static Wordlist English
		{
			get
			{
				if(_English == null)
					_English = LoadWordList(Language.English).Result;
				return _English;
			}
		}


		public static Task<Wordlist> LoadWordList(Language language)
		{
			string name = GetLanguageFileName(language);
			return LoadWordList(name);
		}

		internal static string GetLanguageFileName(Language language)
		{
			string name = null;
			switch(language)
			{
				case Language.ChineseTraditional:
					name = "chinese_traditional";
					break;
				case Language.ChineseSimplified:
					name = "chinese_simplified";
					break;
				case Language.English:
					name = "english";
					break;
				case Language.Japanese:
					name = "japanese";
					break;
				case Language.Spanish:
					name = "spanish";
					break;
				default:
					throw new NotSupportedException(language.ToString());
			}
			return name;
		}

		static Dictionary<string, Wordlist> _LoadedLists = new Dictionary<string, Wordlist>();
		public static async Task<Wordlist> LoadWordList(string name)
		{
			if(name == null)
				throw new ArgumentNullException("name");
			Wordlist result = null;
			lock(_LoadedLists)
			{
				_LoadedLists.TryGetValue(name, out result);
			}
			if(result != null)
				return await Task.FromResult<Wordlist>(result).ConfigureAwait(false);


			if(WordlistSource == null)
				throw new InvalidOperationException("Wordlist.WordlistSource is not set, impossible to fetch word list.");
			result = await WordlistSource.Load(name).ConfigureAwait(false);
			if(result != null)
				lock(_LoadedLists)
				{
					_LoadedLists.AddOrReplace(name, result);
				}
			return result;
		}

		public static IWordlistSource WordlistSource
		{
			get;
			set;
		}

		private String[] _words;

		/// <summary>
		/// Constructor used by inheritence only
		/// </summary>
		/// <param name="words">The words to be used in the wordlist</param>
		public Wordlist(String[] words, char space, string name)
		{
			_words = words
						.Select(w => Mnemonic.NormalizeString(w))
						.ToArray();
			_Space = space;
			_Name = name;
		}

		private readonly string _Name;
		public string Name
		{
			get
			{
				return _Name;
			}
		}
		private readonly char _Space;
		public char Space
		{
			get
			{
				return _Space;
			}
		}

		/// <summary>
		/// Method to determine if word exists in word list, great for auto language detection
		/// </summary>
		/// <param name="word">The word to check for existence</param>
		/// <returns>Exists (true/false)</returns>
		public bool WordExists(string word, out int index)
		{
			word = Mnemonic.NormalizeString(word);
			if(_words.Contains(word))
			{
				index = Array.IndexOf(_words, word);
				return true;
			}

			//index -1 means word is not in wordlist
			index = -1;
			return false;
		}

		/// <summary>
		/// Returns a string containing the word at the specified index of the wordlist
		/// </summary>
		/// <param name="index">Index of word to return</param>
		/// <returns>Word</returns>
		public string GetWordAtIndex(int index)
		{
			return _words[index];
		}

		/// <summary>
		/// The number of all the words in the wordlist
		/// </summary>
		public int WordCount
		{
			get
			{
				return _words.Length;
			}
		}


		public static Task<Wordlist> AutoDetectAsync(string sentence)
		{
			return LoadWordList(AutoDetectLanguage(sentence));
		}
		public static Wordlist AutoDetect(string sentence)
		{
			return LoadWordList(AutoDetectLanguage(sentence)).Result;
		}
		public static Language AutoDetectLanguage(string[] words)
		{
			List<int> languageCount = new List<int>(new int[] { 0, 0, 0, 0, 0 });
			int index;

			foreach(string s in words)
			{
				if(Wordlist.English.WordExists(s, out index))
				{
					//english is at 0
					languageCount[0]++;
				}

				if(Wordlist.Japanese.WordExists(s, out index))
				{
					//japanese is at 1
					languageCount[1]++;
				}

				if(Wordlist.Spanish.WordExists(s, out index))
				{
					//spanish is at 2
					languageCount[2]++;
				}

				if(Wordlist.ChineseSimplified.WordExists(s, out index))
				{
					//chinese simplified is at 3
					languageCount[3]++;
				}

				if(Wordlist.ChineseTraditional.WordExists(s, out index) && !Wordlist.ChineseSimplified.WordExists(s, out index))
				{
					//chinese traditional is at 4
					languageCount[4]++;
				}
			}

			//no hits found for any language unknown
			if(languageCount.Max() == 0)
			{
				return Language.Unknown;
			}

			if(languageCount.IndexOf(languageCount.Max()) == 0)
			{
				return Language.English;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 1)
			{
				return Language.Japanese;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 2)
			{
				return Language.Spanish;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 3)
			{
				if(languageCount[4] > 0)
				{
					//has traditional characters so not simplified but instead traditional
					return Language.ChineseTraditional;
				}

				return Language.ChineseSimplified;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 4)
			{
				return Language.ChineseTraditional;
			}

			return Language.Unknown;
		}
		public static Language AutoDetectLanguage(string sentence)
		{
			string[] words = sentence.Split(new char[] { ' ', '　' }); //normal space and JP space

			return AutoDetectLanguage(words);
		}

		public string[] Split(string mnemonic)
		{
			return mnemonic.Split(new char[]{Space}, StringSplitOptions.RemoveEmptyEntries);
		}

		public override string ToString()
		{
			return _Name;
		}

		public string[] GetWords(int[] indices)
		{
			return
				indices
				.Select(i => GetWordAtIndex(i))
				.ToArray();
		}

		public string GetSentence(int[] indices)
		{
			return String.Join(Space.ToString(), GetWords(indices));
					
		}

		public int[] GetIndices(string[] words)
		{
			var indices = new int[words.Length];
			for(int i = 0 ; i < words.Length ; i++)
			{
				int idx = -1;

				if(!WordExists(words[i], out idx))
				{
					throw new FormatException("Word " + words[i] + " is not in the wordlist for this language, cannot continue to rebuild entropy from wordlist");
				}
				indices[i] = idx;
			}
			return indices;
		}
	}
}