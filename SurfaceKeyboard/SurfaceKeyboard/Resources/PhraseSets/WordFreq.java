import java.io.*;
import java.util.*;

/** This program creates a list of words and word frequencies
* from an input text file.<p>
*
* Output is written to the console.<p>
*
* Invocation:<p>
*
* <pre>
*     PROMPT>java WordFreq file [-w] [-f] [-v] [-s] [-x] [-l] 
*
*     where file = input text file
*
*     options: (default is no output)
*     -w = output words
*     -f = output frequencies
*     -v = verbose option
*     -s = sort 'by word' (default is 'by count')
*     -x = exclude words with characters other than a-z or A-Z
*     -l = convert all characters to lowercase
* </pre>
*
* The input text is tokenized
* using Java's <code>StringTokenizer</code> class with "\n\t\" .,-?;:()!"
* as the delimiter string.  Note the omission of the single quote character
* as a deliminter.  As an example, the text
*
* <pre>
*     Free-range chickens are the best, don't you think?
* </pre>
*
* is broken into the following tokens:
*
* <pre>
*     Free
*     range
*     chickens
*     are
*     the
*     best
*     don't
*     you
*     think
* </pre>
*
* Here are some example invocations:<p>
*
* <pre>
*     PROMPT>java WordFreq phrases2.txt -w -f -v
*     the 189
*     a 108
*     is 85
*     to 57
*     of 54
*     you 49
*     ...
*     yes 1
*     yet 1
*     young 1
*     zoom 1
*     total words: 2713
*     unique words: 1164
*     non-words: 0
*
*     PROMPT>java WordFreq GreatExpectations.txt -v -x
*     total words: 183958
*     unique words: 11509
*     non-words: 2735
*
*     PROMPT>type temp.txt
*     Hello World
*     hello world
*
*     PROMPT>java WordFreq temp.txt -w -f
*     Hello 1
*     World 1
*     hello 1
*     world 1
*
*     PROMPT>java WordFreq temp.txt -w -f -l
*     hello 2
*     world 2
* </pre>
*
* @author Scott MacKenzie, 2001
*/
public class WordFreq
{
   public static void main(String[] args) throws IOException
   {
      // check usage
      if (args.length == 0)
      {
         usage();
         return;
      }

      // declare and set command-line options
      boolean wordOption = false;
      boolean freqOption = false;
      boolean verboseOption = false;
      boolean sortOption = false;
      boolean excludeOption = false;
      boolean lowercaseOption = false;
      for (int i = 0; i < args.length; ++i)
      {
         if (args[i].equals("-w")) wordOption = true;
         if (args[i].equals("-f")) freqOption = true;
         if (args[i].equals("-v")) verboseOption = true;
         if (args[i].equals("-s")) sortOption = true;
         if (args[i].equals("-x")) excludeOption = true;        
         if (args[i].equals("-l")) lowercaseOption = true;        
      }

      // open text file for input
      BufferedReader stdin = new BufferedReader(new FileReader(args[0]));

      // these characters are potential delimiters of words
      // Note: single quote is omitted
      String delim = "\n\t\" .,-?;:()!";

      // process lines until no more input
      TokenFreq tf = new TokenFreq();
      String line;
      int excludeTally = 0;
      while ((line = stdin.readLine()) != null)
      {
         StringTokenizer st = new StringTokenizer(line, delim);
         while (st.hasMoreTokens())
         {
            String t = st.nextToken();

            if (lowercaseOption)
               t = t.toLowerCase();

            if (!MyUtil.isWord(t) && excludeOption)
               ++excludeTally;
            else
               tf.addToken(t);
         }
      }

      Token[] tArray = tf.getTokens();

      // sort by word or count
      if (sortOption)
         Arrays.sort(tArray, new TokenByWord());
      else
         Arrays.sort(tArray, new TokenByCount());

      int tally = 0;
      for (int i = 0; i < tArray.length; ++i)
      {
         if (wordOption)
            System.out.print(tArray[i].getToken());
         if (freqOption)
            System.out.print(" " + tArray[i].getCount());
         if (wordOption || freqOption)
            System.out.println();
         tally += tArray[i].getCount();
      }

      if (verboseOption)
      {
         System.out.println("total words: " + tally);
         System.out.println("unique words: " + tArray.length);
         System.out.println("non-words: " + excludeTally);
      }
   }

   private static void usage()
   {
      System.out.println(
         "usage: java WordFreq file [-w] [-f] [-v] [-s] [-x]\n" +
         "\n" +
         "   where 'file' is a text file\n" +
         "\n" +
         "   options: (default is no output)\n" +
         "   -w = output words\n" +
         "   -f = output word frequencies\n" +
         "   -v = verbose output\n" +
         "   -s = sort 'by word' (default is 'by frequency')\n" +
         "   -x = exclude words with characters other than a-z or A-Z\n" +
         "   -l = convert all characters to lowercase"
      );
   }
}

