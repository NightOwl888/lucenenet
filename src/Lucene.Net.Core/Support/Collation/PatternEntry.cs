using System;
using System.Text;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// Utility class for normalizing and merging patterns for collation.
    /// This is to be used with MergeCollation for adding patterns to an
    /// existing rule table.
    /// </summary>
    internal class PatternEntry
    {
        /**
     * Gets the current extension, quoted
     */
        public void AppendQuotedExtension(StringBuilder toAddTo)
        {
            AppendQuoted(extension, toAddTo);
        }

        /**
         * Gets the current chars, quoted
         */
        public void AppendQuotedChars(StringBuilder toAddTo)
        {
            AppendQuoted(chars, toAddTo);
        }

        /**
         * WARNING this is used for searching in a Vector.
         * Because Vector.indexOf doesn't take a comparator,
         * this method is ill-defined and ignores strength.
         */
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            PatternEntry other = (PatternEntry)obj;
            bool result = chars.Equals(other.chars);
            return result;
        }

        public override int GetHashCode()
        {
            return chars.GetHashCode();
        }

        /**
         * For debugging.
         */
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            AddToBuffer(result, true, false, null);
            return result.ToString();
        }

        /**
         * Gets the strength of the entry.
         */
        internal CollatorStrength Strength
        {
            get { return strength; }
        }

        /**
         * Gets the expanding characters of the entry.
         */
        internal string Extension
        {
            get { return extension; }
        }

        /**
         * Gets the core characters of the entry.
         */
        internal string Chars
        {
            get { return chars; }
        }

        // ===== privates =====

        internal void AddToBuffer(StringBuilder toAddTo,
                         bool showExtension,
                         bool showWhiteSpace,
                         PatternEntry lastEntry)
        {
            if (showWhiteSpace && toAddTo.Length > 0)
                if (strength == CollatorStrength.PRIMARY || lastEntry != null)
                    toAddTo.Append('\n');
                else
                    toAddTo.Append(' ');
            if (lastEntry != null)
            {
                toAddTo.Append('&');
                if (showWhiteSpace)
                    toAddTo.Append(' ');
                lastEntry.AppendQuotedChars(toAddTo);
                AppendQuotedExtension(toAddTo);
                if (showWhiteSpace)
                    toAddTo.Append(' ');
            }
            switch ((int)strength)
            {
                case (int)CollatorStrength.IDENTICAL: toAddTo.Append('='); break;
                case (int)CollatorStrength.TERTIARY: toAddTo.Append(','); break;
                case (int)CollatorStrength.SECONDARY: toAddTo.Append(';'); break;
                case (int)CollatorStrength.PRIMARY: toAddTo.Append('<'); break;
                case RESET: toAddTo.Append('&'); break;
                case UNSET: toAddTo.Append('?'); break;
            }
            if (showWhiteSpace)
                toAddTo.Append(' ');
            AppendQuoted(chars, toAddTo);
            if (showExtension && extension.Length != 0)
            {
                toAddTo.Append('/');
                AppendQuoted(extension, toAddTo);
            }
        }

        static void AppendQuoted(string chars, StringBuilder toAddTo)
        {
            bool inQuote = false;
            char ch = chars[0];
            if (char.IsSeparator(ch))
            {
                inQuote = true;
                toAddTo.Append('\'');
            }
            else
            {
                if (PatternEntry.IsSpecialChar(ch))
                {
                    inQuote = true;
                    toAddTo.Append('\'');
                }
                else
                {
                    switch (ch)
                    {
                        case (char)0x0010:
                        case '\f':
                        case '\r':
                        case '\t':
                        case '\n':
                        case '@':
                            inQuote = true;
                            toAddTo.Append('\'');
                            break;
                        case '\'':
                            inQuote = true;
                            toAddTo.Append('\'');
                            break;
                        default:
                            if (inQuote)
                            {
                                inQuote = false; toAddTo.Append('\'');
                            }
                            break;
                    }
                }
            }
            toAddTo.Append(chars);
            if (inQuote)
                toAddTo.Append('\'');
        }

        //========================================================================
        // Parsing a pattern into a list of PatternEntries....
        //========================================================================

        internal PatternEntry(CollatorStrength strength,
                     StringBuilder chars,
                     StringBuilder extension)
        {
            this.strength = strength;
            this.chars = chars.ToString();
            this.extension = (extension.Length > 0) ? extension.ToString()
                                                      : "";
        }

        internal class Parser
        {
            private string pattern;
            private int i;

            public Parser(string pattern)
            {
                this.pattern = pattern;
                this.i = 0;
            }

            public PatternEntry Next()
            {
                CollatorStrength newStrength = (CollatorStrength)UNSET;

                newChars.Length = 0;
                newExtension.Length = 0;

                bool inChars = true;
                bool inQuote = false;
                
                while (i < pattern.Length)
                {
                    char ch = pattern[i];
                    if (inQuote)
                    {
                        if (ch == '\'')
                        {
                            inQuote = false;
                        }
                        else
                        {
                            if (newChars.Length == 0) newChars.Append(ch);
                            else if (inChars) newChars.Append(ch);
                            else newExtension.Append(ch);
                        }
                    }
                    else switch (ch)
                        {
                            case '=':
                                if ((int)newStrength != UNSET) goto mainLoop;
                                newStrength = CollatorStrength.IDENTICAL; break;
                            case ',':
                                if ((int)newStrength != UNSET) goto mainLoop;
                                newStrength = CollatorStrength.TERTIARY; break;
                            case ';':
                                if ((int)newStrength != UNSET) goto mainLoop;
                                newStrength = CollatorStrength.SECONDARY; break;
                            case '<':
                                if ((int)newStrength != UNSET) goto mainLoop;
                                newStrength = CollatorStrength.PRIMARY; break;
                            case '&':
                                if ((int)newStrength != UNSET) goto mainLoop;
                                newStrength = (CollatorStrength)RESET; break;
                            case '\t':
                            case '\n':
                            case '\f':
                            case '\r':
                            case ' ': break; // skip whitespace TODO use Character
                            case '/': inChars = false; break;
                            case '\'':
                                inQuote = true;
                                ch = pattern[++i];
                                if (newChars.Length == 0) newChars.Append(ch);
                                else if (inChars) newChars.Append(ch);
                                else newExtension.Append(ch);
                                break;
                            default:
                                if ((int)newStrength == UNSET)
                                {
                                    throw new FormatException
                                        ("missing char (=,;<&) : " +
                                         pattern.Substring(i,
                                            (i + 10 < pattern.Length) ?
                                             10 : pattern.Length)/*,
                             i*/);
                                }
                                if (PatternEntry.IsSpecialChar(ch) && (inQuote == false))
                                    throw new FormatException
                                        ("Unquoted punctuation character : " + Convert.ToString(ch, 16) /*, i*/);
                                if (inChars)
                                {
                                    newChars.Append(ch);
                                }
                                else
                                {
                                    newExtension.Append(ch);
                                }
                                break;
                        }
                    i++;
                }
                mainLoop:
                if ((int)newStrength == UNSET)
                    return null;
                if (newChars.Length == 0)
                {
                    throw new FormatException
                        ("missing chars (=,;<&): " +
                          pattern.Substring(i,
                              (i + 10 < pattern.Length) ?
                               10 : pattern.Length)/*,
                     i*/);
                }

                return new PatternEntry(newStrength, newChars, newExtension);
            }

            // We re-use these objects in order to improve performance
            private StringBuilder newChars = new StringBuilder();
            private StringBuilder newExtension = new StringBuilder();

        }

        static bool IsSpecialChar(char ch)
        {
            return ((ch == '\u0020') ||
                    ((ch <= '\u002F') && (ch >= '\u0022')) ||
                    ((ch <= '\u003F') && (ch >= '\u003A')) ||
                    ((ch <= '\u0060') && (ch >= '\u005B')) ||
                    ((ch <= '\u007E') && (ch >= '\u007B')));
        }


        internal const int RESET = -2;
        internal const int UNSET = -1;

        internal CollatorStrength strength = (CollatorStrength)UNSET;
        internal string chars = "";
        internal string extension = "";
    }
}
