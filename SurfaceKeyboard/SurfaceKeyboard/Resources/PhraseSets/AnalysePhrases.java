import java.io.*;
import java.util.*;

/** This program analyses phrases used for text entry experiments.<p>
*
* Invocation:<p>
*
* <pre>
*     PROMPT>java AnalysePhrases file
*
*     where file = name of phrase file to analyse
* </pre>
*
* Here is an example invocation:<p>
*
* <pre>
*     PROMPT>java AnalysePhrases phrases2.txt
*     ---------------------------------------
*     phrases: 500
*     minimum length: 16
*     maximum length: 43
*     average phrase length: 28.6
*     ---------------------------------------
*     words: 2711
*     minimum length: 1
*     maximum length: 13
*     average word length: 4.45
*     words containing non-letters: 0
*     ---------------------------------------
*     letters: 14301
*     correlation with English: 0.9541
*     ---------------------------------------
* </pre>
*
* The correlation with English is computed against the letter
* frequencies given by Mayzner and Tresselt (1965).<p>
*
* @author Scott MacKenzie, 2001                             
*/
public class AnalysePhrases
{
   public static void main(String[] args) throws IOException
   {
      if (args.length == 0)
      {
         System.out.println(
            "Usage: java AnalysePhrases file\n\n" +
            "where file = a file containing text phrases");
         System.exit(0);
      }

      // open text file for input
      BufferedReader stdin = new BufferedReader(new FileReader(args[0]));

      String phrase;
      String word;
      StringTokenizer st;
      int phraseCount = 0;
      int phraseLength = 0;
      int phraseMax = 0;
      int phraseMin = Integer.MAX_VALUE;
      int wordCount = 0;
      int wordLength = 0;
      int letterCount = 0;
      int wordMax = 0;
      int wordMin = Integer.MAX_VALUE;
      int nonWord = 0;
      int wordsPerPhrase = 0;
      int lettersPerWord = 0;
      TokenFreq tf = new TokenFreq();
      TokenFreq tfWords = new TokenFreq();

      // letter probabilities (a-z) from Mayzner & Tresselt (1965)
      double[] letterProb = {
         0.0810, 0.0163, 0.0236, 0.0432, 0.1132, 0.0179,
         0.0218, 0.0772, 0.0515, 0.0015, 0.0107, 0.0447,
         0.0248, 0.0601, 0.0663, 0.0153, 0.0008, 0.0589,
         0.0607, 0.0978, 0.0309, 0.0099, 0.0287, 0.0014,
         0.0212, 0.0006
      };

      // process lines until no more input
      while ((phrase = stdin.readLine()) != null)
      {
         letterCount += phrase.length();
         ++phraseCount;
         phraseLength += phrase.length();
         if (phrase.length() > phraseMax) phraseMax = phrase.length();
         if (phrase.length() < phraseMin) phraseMin = phrase.length();
         st = new StringTokenizer(phrase);
         while (st.hasMoreTokens())
         {
            word = st.nextToken();

            tfWords.addToken(word); // need to determine unique words

            ++wordCount;
            wordLength += word.length();
            if (word.length() > wordMax) wordMax = word.length();
            if (word.length() < wordMin) wordMin = word.length();
            if (!MyUtil.isWord(word)) ++nonWord;
         }

         // now work on letters
         for (int i = 0; i < phrase.length(); ++i)
         {
            String letter = phrase.substring(i, i + 1);
            tf.addToken(letter);
         }
      }

      // print the results
      System.out.println("---------------------------------------");
      System.out.println("phrases: " + phraseCount);
      System.out.println("minimum length: " + phraseMin);
      System.out.println("maximum length: " + phraseMax);
      double d = (double)phraseLength / phraseCount;
      d = MyUtil.trim(d, 2);
      System.out.println("average phrase length: " + d);

      System.out.println("---------------------------------------");
      System.out.println("words: " + wordCount);
      System.out.println("unique words: " + tfWords.size());
      System.out.println("minimum length: " + wordMin);
      System.out.println("maximum length: " + wordMax);
      d = (double)wordLength / wordCount;
      d = MyUtil.trim(d, 2);
      System.out.println("average word length: " + d);
      System.out.println("words containing non-letters: " + nonWord);
      System.out.println("---------------------------------------");

      System.out.println("letters: " + letterCount);

      Token[] tArray = tf.getTokens();
      double[] letterFreq = new double[26];

      for (int i = 0; i < 26; ++i)
         for (int idx = 0; idx < tArray.length; ++idx)
         {
            if (tArray[idx].getToken().equals("" + (char)('a' + i)))
            {
               letterFreq[i] = tArray[idx].getCount();
               break;
            }
         }

      double r = MyUtil.corr(letterFreq, letterProb, 26);
      r = MyUtil.trim(r, 4);

      System.out.println("correlation with English: " + r);
      System.out.print("---------------------------------------");

   }
}
