using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pixel3D.Strings
{
	public static class Tokenizer
    {
        #region Reading

        public static List<string[]> Tokenize(string[] lines, List<string> errors, ref bool foundError)
        {
            List<string> workingLine = new List<string>();
            StringBuilder workingToken = new StringBuilder();


            List<string[]> result = new List<string[]>(lines.Length);

            for(int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                var input = lines[lineNumber];

                workingLine.Clear();
                workingToken.Clear();

                bool inQuotes = false;
                bool escape = false;

                for(int i = 0; i < input.Length; i++)
                {
                    bool finishToken = false;

                    if(inQuotes)
                    {
                        if(escape)
                        {
                            escape = false;
                            switch(input[i])
                            {
                                case '\\': workingToken.Append('\\'); break;
                                case 'n': workingToken.Append('\n'); break;
                                case '\"': workingToken.Append('\"'); break;

                                default:
                                    errors.Add("ERROR: Unknown escape token '" + input[i] + "', line " + lineNumber + ", position " + i + ".");
                                    foundError = true;
                                    break;
                            }
                        }
                        else
                        {
                            switch(input[i])
                            {
                                case '\\':
                                    escape = true;
                                    break;

                                case '\"':
                                    inQuotes = false;
                                    finishToken = true;
                                    break;

                                default:
                                    workingToken.Append(input[i]);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if(char.IsWhiteSpace(input[i]))
                        {
                            finishToken = true;
                        }
                        else if(input[i] == '#')
                        {
                            break;
                        }
                        else if(input[i] == '\"')
                        {
                            inQuotes = true;
                        }
                        else
                        {
                            workingToken.Append(input[i]);
                        }
                    }

                    if(finishToken && workingToken.Length > 0)
                    {
                        workingLine.Add(workingToken.ToString());
                        workingToken.Clear();
                    }
                }

                // Handle final token:
                if(workingToken.Length > 0)
                {
                    workingLine.Add(workingToken.ToString());
                    workingToken.Clear();
                }

                if(workingLine.Count > 0)
                    result.Add(workingLine.ToArray());
            }

            return result;
        }

        #endregion
		
        #region Writing

        public static string AsToken(string token)
        {
            for(int i = 0; i < token.Length; i++)
            {
                switch(token[i])
                {
                    case '#':
                    case '\\':
                    case '"':
                        return QuoteWrapAndEscape(token, i);

                    default:
                        if(char.IsWhiteSpace(token[i]))
                            return QuoteWrapAndEscape(token, i);
                        break;
                }
            }

            return token;
        }

        private static string QuoteWrapAndEscape(string token, int startIndex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('"');
            sb.Append(token, 0, startIndex);
            for(int i = startIndex; i < token.Length; i++)
            {
                switch(token[i])
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': break;
                    default:
                        sb.Append(token[i]);
                        break;
                }
            }
            sb.Append('"');

            return sb.ToString();
        }



        public static void WriteLine(StreamWriter stream, string firstToken, params string[] tokens)
        {
            stream.Write(firstToken);

            for(int t = 0; t < tokens.Length; t++)
            {
                stream.Write(' ');
                stream.Write(AsToken(tokens[t]));
            }

            stream.WriteLine();
        }

        public static void Write(StreamWriter stream, string firstToken, params string[] tokens)
        {
            stream.Write(firstToken);

            for(int t = 0; t < tokens.Length; t++)
            {
                stream.Write(' ');
                stream.Write(AsToken(tokens[t]));
            }

            stream.Write(' '); // <- in case more tokens are written
        }


        #endregion
		
        #region Binary Read/Write

        public static List<string[]> BinaryRead(BinaryReader br)
        {
            int lineCount = br.ReadInt32();
            List<string[]> lines = new List<string[]>(lineCount);

            for(int l = 0; l < lineCount; l++)
            {
                int tokenCount = br.ReadInt32();
                string[] tokens = new string[tokenCount];
                for(int t = 0; t < tokenCount; t++)
                {
                    tokens[t] = br.ReadString();
                }
                lines.Add(tokens);
            }

            return lines;
        }

        public static void BinaryWrite(BinaryWriter bw, List<string[]> tokenizedLines)
        {
            bw.Write(tokenizedLines.Count);
            foreach(var tokenizedLine in tokenizedLines)
            {
                bw.Write(tokenizedLine.Length);
                foreach(var token in tokenizedLine)
                {
                    Debug.Assert(token != null);
                    bw.Write(token);
                }
            }
        }

        #endregion
    }
}
