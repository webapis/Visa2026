using System;
using System.Globalization;
foreach (var n in new[] { "en-US", "tr-TR", "tk-TM", "ru-RU", "tk" }) {
  try { Console.WriteLine(n + " OK " + CultureInfo.GetCultureInfo(n).Name); }
  catch (Exception ex) { Console.WriteLine(n + " FAIL " + ex.Message); }
}
