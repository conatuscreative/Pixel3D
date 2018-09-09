using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.Engine
{
    public static class LineWrapping
    {
        public static void Wrap(string input, StringBuilder output, int availableWidth, SpriteFont font, StringBuilder workingSpace, bool optimise)
        {
            if(optimise)
                WrapOptimise(input, output, availableWidth, font, workingSpace);
            else
                WrapInternal(input, output, (float)availableWidth, font, workingSpace);
        }

        /// <summary>Attempt to wrap text into the given available width, while trying to keep all lines more-or-less even</summary>
        public static void WrapOptimise(string input, StringBuilder output, int availableWidth, SpriteFont font, StringBuilder workingSpace)
        {
            float maxBlockWidth = (float)availableWidth;

            WrapResult initialResult = WrapInternal(input, output, maxBlockWidth, font, workingSpace);
            if(initialResult.lineCount <= 1)
                return; // We can never waste space with a single line


            // At this point, we have some excess space available at the end of the line,
            // which we can use up by pushing some words down by iterativly reducing the width.
            //
            // There is maybe some concerns about opening up a larger gap on a prior line,
            // but I'm (-AR) not sure that this is a problem in practice. Won't worry about it for now.
            // (Practical results indicate the main problem is not enough iterations.)


            // Determine a threshold to stop doing trials:
            workingSpace.Clear();
            workingSpace.EnsureCapacity(3);
            workingSpace.Append(' ');
            workingSpace.Append(' ');
            float zeroSpace = font.MeasureString(workingSpace).X;
            workingSpace.Append(' '); // <- NOTE: Width of a space used as exit threshold
            float exitThreshold = (font.MeasureString(workingSpace).X - zeroSpace) * 0.5f;
            exitThreshold = 1;

            float minBlockWidth = 0;
            bool lastResultGood = true;
            WrapResult latestResult = initialResult;

            // NOTE: Keep iteration count capped to keep wrap algorithm O(n)
            int i = 6; // <- Hand-tuned value
            while(true)
            {
                if(i == 0)
                    break;
                if(maxBlockWidth - minBlockWidth < exitThreshold) // <- TODO: Should probably switch to linear search at this point? (use Floor(max) or Ceiling(min) and go from there with 1px resolution?)
                    break;

                // Determine a new width to trial:
                float trialWidth;
                if(lastResultGood)
                {
                    float lastTrailingSpace = (maxBlockWidth - latestResult.finalLineWidth);
                    if(lastTrailingSpace <= exitThreshold)
                        break;

                    trialWidth = maxBlockWidth - (lastTrailingSpace / (float)(initialResult.lineCount)); // <- TODO: More carefully check this improves results or iteration count.

                    if(trialWidth <= minBlockWidth + exitThreshold)
                        trialWidth = (minBlockWidth + maxBlockWidth) * 0.5f;
                }
                else
                    trialWidth = (minBlockWidth + maxBlockWidth) * 0.5f; // <- binary search

                latestResult = WrapInternal(input, output, trialWidth, font, workingSpace);


                lastResultGood = (latestResult.lineCount == initialResult.lineCount);
                if(lastResultGood)
                    maxBlockWidth = trialWidth; // <- We fit in this much, maybe we can get smaller
                else
                    minBlockWidth = trialWidth; // <- We didn't fit, so we must be wider

                i--;
            }

            // Ensure we exit with output in a known-good state:
            if(!lastResultGood)   
                WrapInternal(input, output, maxBlockWidth, font, workingSpace);
        }


        private struct WrapResult
        {
            public int lineCount;
            public float finalLineWidth;
        }



        enum CharacterClass
        {
            Whitespace,
            Unspecified,
            Leading,
            Following,
            Joining,
            Hiragana,
            Katakana,
            Ideograph,
        }

        private static CharacterClass Classify(char c)
        {
            if(char.IsWhiteSpace(c))
                return CharacterClass.Whitespace;

            if(Array.BinarySearch(Leading, c) >= 0)
                return CharacterClass.Leading;
            if(Array.BinarySearch(Following, c) >= 0)
                return CharacterClass.Following;
            if(Array.BinarySearch(Joining, c) >= 0)
                return CharacterClass.Joining;

            // Try to keep Hiragana and Katakana words together:
            if(c >= '\u3040' && c <= '\u309F')
                return CharacterClass.Hiragana;
            if(c >= '\u30A0' && c <= '\u30FF')
                return CharacterClass.Katakana;

            if(c >= '\u4E00' && c <= '\u9FFF') // <- CJK Unified Ideographs
                return CharacterClass.Ideograph;
            if(c >= '\u3400' && c <= '\u4DBF') // <- CJK Unified Ideographs Extension A
                return CharacterClass.Ideograph;

            if(c >= '\uE000' && c <= '\uF8FF') // <- Private use block (we use this for buttons)
                return CharacterClass.Ideograph;

            // NOTE: Rely on manual spacing for Latin words
            //       (Pretty sure this covers full-width latin characters, etc - rely on adjacent CJK to break these)
            return CharacterClass.Unspecified;
        }


        // Originally from https://en.wikipedia.org/wiki/Line_breaking_rules_in_East_Asian_languages#Line_breaking_rules_in_Japanese_text_.28Kinsoku_Shori.29
        // TODO: Check we're including all characters from the Japanese list here: https://msdn.microsoft.com/en-us/goglobal/bb688158.aspx

        private const string ClosingBrackets = ")]｝〕〉》」』】〙〗〟’ ｠»）";
        private const string JapaneseDisallowedStartingCharacters = "ヽヾーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ々〻";
        private const string Hyphens = "‐゠–〜";
        private const string Delimiters = " ?!‼⁇⁈⁉！";
        private const string MidSentencePunctuation = "・、:;,";
        private const string SentenceEndingPunctuation = "。.";
        private static readonly char[] Following = (ClosingBrackets + JapaneseDisallowedStartingCharacters
                + Hyphens + Delimiters + MidSentencePunctuation + SentenceEndingPunctuation).OrderBy(c => c).ToArray();

        private const string OpeningBrackets = "([｛〔〈《「『【〘〖〝‘｟«（";
        private static readonly char[] Leading = (OpeningBrackets).OrderBy(c => c).ToArray();

        private const string InseparableCharacters = "—…‥〳〴〵\"\'";
        private static readonly char[] Joining = (InseparableCharacters).OrderBy(c => c).ToArray();


        private static WrapResult WrapInternal(string input, StringBuilder output, float availableWidth, SpriteFont font, StringBuilder workingSpace)
        {
            // This is a dumb thing we have to do because of XNA. We can't get the A+B+C width any other way.
            // (Not that we use A or B widths in our font, but it's future-proof.)
            workingSpace.Clear();
            workingSpace.EnsureCapacity(3);
            workingSpace.Append(' ');
            workingSpace.Append(' ');
            float zeroSpace = font.MeasureString(workingSpace).X;
            workingSpace.Append(' ');


            WrapResult result;
            result.lineCount = 1;
            result.finalLineWidth = 0;

            // Because maybe we want to append to the output
            output.Clear();
            output.EnsureCapacity(input.Length);
            float lineWidth = 0;

            bool firstWordOnLine = true;
            int? lastWhitespaceStart = null;

            int wordStart = 0;
            float wordWidth = 0;
            CharacterClass wordClass = 0;

            for(int i = 0; i < input.Length; i++)
            {
                if(input[i] == '\r')
                {
                    // Skip.
                }
                else if(input[i] == '\n')
                {
                    // Break immediately
                    if(wordClass != CharacterClass.Whitespace)
                        output.Append(input, wordStart, i - wordStart + 1); // <- +1 to append the newline as well
                    else
                        output.Append('\n');
                    result.lineCount++;
                    wordStart = i+1;
                    wordWidth = lineWidth = 0;
                    firstWordOnLine = true;
                    lastWhitespaceStart = null;
                }
                else
                {
                    CharacterClass characterClass = Classify(input[i]);

                    if(i == wordStart) // <- First character of the word gets to categorise
                    {
                        wordClass = characterClass;
                    }
                    else
                    {
                        bool endPreviousWord = (characterClass != wordClass || characterClass == CharacterClass.Ideograph);

                        // Exceptions:
                        if(characterClass != CharacterClass.Whitespace && wordClass != CharacterClass.Whitespace) // <- no exceptions for whitespace!
                        {
                            if(characterClass == CharacterClass.Following
                                    || characterClass == CharacterClass.Joining
                                    || wordClass == CharacterClass.Joining
                                    || wordClass == CharacterClass.Leading)
                                endPreviousWord = false;
                        }

                        if(endPreviousWord)
                        {
                            // If this word was whitespace, and the next word wraps, we'll want to skip the excess whitespace
                            if(wordClass == CharacterClass.Whitespace)
                                lastWhitespaceStart = output.Length;
                            else
                                lastWhitespaceStart = null;

                            output.Append(input, wordStart, i - wordStart);
                            lineWidth += wordWidth;
                            wordStart = i;
                            wordWidth = 0;
                            firstWordOnLine = false;
                        }

                        wordClass = characterClass;
                    }


                    workingSpace[1] = input[i];
                    float w = font.MeasureString(workingSpace).X - zeroSpace;
                    bool shouldBreak = (lineWidth + wordWidth + w > availableWidth);

                    if(shouldBreak && (firstWordOnLine || wordClass != CharacterClass.Whitespace))
                    {
                        if(firstWordOnLine) // <- Allow mid-word breaking if the word is longer than the line
                        {
                            bool firstCharacterOnLine = (wordStart == i);
                            if(firstCharacterOnLine)
                                i++;

                            if(wordClass != CharacterClass.Whitespace)
                                output.Append(input, wordStart, i - wordStart);
                            output.Append('\n');
                            result.lineCount++;
                            wordStart = i;
                            wordWidth = 0;

                            lineWidth = 0;
                            firstWordOnLine = true;
                            lastWhitespaceStart = null;

                            if(firstCharacterOnLine)
                                continue;
                        }
                        else
                        {
                            if(lastWhitespaceStart.HasValue)
                                output.Remove(lastWhitespaceStart.Value, output.Length - lastWhitespaceStart.Value);

                            output.Append('\n');
                            result.lineCount++;
                            lineWidth = 0;
                            firstWordOnLine = true;
                            lastWhitespaceStart = null;
                        }
                    }

                    wordWidth += w;
                }
            }

            // Append the final word, skip if whitespace
            if(wordClass == CharacterClass.Whitespace)
                wordWidth = 0;
            else
                output.Append(input, wordStart, input.Length - wordStart);

            result.finalLineWidth = lineWidth + wordWidth;

            return result;
        }



        /// <summary>Given two strings that are the same except for whitespace, return the index in the target that matches the index in the source</summary>
        public static int GetMatchingStringIndexIgnoreWhitespace(StringBuilder source, StringBuilder target, int index)
        {
            if(index >= source.Length)
                return target.Length;

            if(target.Length == 0)
                return 0;

            int d = 0;
            for(int i = 0; i < index; i++)
            {
                bool sourceWhitespace = char.IsWhiteSpace(source[i]);
                bool targetWhitespace = char.IsWhiteSpace(target[d]);

                if(sourceWhitespace && targetWhitespace) // <- both whitespace (don't care if it's not the same character)
                {
                    d++;
                    if(d >= target.Length)
                        break;
                    continue;
                }
                else if(sourceWhitespace) // <- source has whitespace to skip over
                {
                    continue;
                }
                else
                    while(targetWhitespace) // <- destination has whitespace to skip over
                    {
                        d++;
                        if(d >= target.Length)
                            break;
                        targetWhitespace = char.IsWhiteSpace(target[d]);
                    }

                // Now we are at a non-whitespace character on both strings, see if it matches (it really should!!)
                Debug.Assert(source[i] == target[d]); // <- You have two mismatched strings!
                if(source[i] == target[d])
                {
                    d++;
                    if(d >= target.Length)
                        break;
                    continue;
                }
            }

            return d;
        }

    }
}
