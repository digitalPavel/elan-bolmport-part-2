using System.Net; 
using System.Text;
using Newtonsoft.Json;
using OrderProperty;

namespace JsonParsing;

public class JsonParsing
{
    // Added -- on command line switch. It's fairly standard to have single dash for switches that are single letters
    // and double dashes for "named" switches.
    const string filterFlag = "--coid=";
    static void Main(string[] args)
    {

        string? result;
        string coid = GetCoIdFromCommandLineArgs(args);

        // Added check for missing argument
        if (coid == "")
        {
            Terminate(1, "Missing coid argument.\n\nUsage: program.exe--coid = XXX\n\nNon zero return code indicates error.");
        }

        // we'll need to pull this from the database. I'll get you a stored procedure for it eventually.
        string url = "https://stybi.navigahub.com/api/queries/sty%3Amnth-sales-for-portal/_execute?output=json&limit=-1&timezone=America%2FNew_York&applyFormatting=true&token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJrZXkiOiI2NDdiMmFhNy0yZjRiLTQxMDctOTRiMC00Mzg2NTBiYjFlYzIiLCJpYXQiOjE2NjA4NTI4MzkuNTg4fQ.sYj3UGT6Af1RNqhPaTiMI8nOSosbEcpTOX1nOforbjs";
        Tuple<string, System.Data.DataSet> dbResult;

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            // converted to "using" statements for WebResponse and stream reader so that you 
            // don't have to manually manage them and close them in a "finally" block.
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    result = reader.ReadLine();

                    if (result == null)
                    {
                        Terminate(1, $"Failed to read line from HTTP response stream.");
                    }

                    List<Order>? orders = JsonConvert.DeserializeObject<List<Order>>(result);

                    // Ensure it was parsed to a useful object.
                    if ((orders == null) || (orders.Count < 1))
                    {
                        Terminate(1, "Failed to find any back order info.");
                    }

                    var db = new Database();
                    
                    bool firstRecord = true;

                    
                    foreach (var order in orders)
                    {
                        if (firstRecord == true)
                        {
                            // Initialize the database import for the first record
                            dbResult = db.GetResults($"catsMonthly_init @coid = '{coid}', @period = '{order.period}'");
                            firstRecord = false;

                            // Check db call was successful and bail if not showing error message on screen and non zero exit code.
                            if (dbResult.Item1 != "")
                            {
                                Terminate(1, $"Failed to execute database call to catsMonthly_init: '{dbResult.Item1}'");
                            }
                        }

                        //  Call procedure with the following parametrs
                        dbResult = db.GetResults($"catsMonthly_save @coid = '{coid}', @period = '{order.period}', @soldUnits = '{order.soldUnits}', @returnedUnits = '{order.returendUnits}', @valueReturend ='{order.valueReturned}', @freeUnits = '{order.freeUnits}'");

                        // Check db call was successful and bail if not showing error message on screen and non zero exit code.
                        if (dbResult.Item1 != "")
                        {
                            Terminate(1, $"Failed to execute database call to catsMonthly_save: '{dbResult.Item1}'");
                        }
                    }
                }
            }
            Terminate();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Gets coid from command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments to check</param>
    /// <returns>The coid if one was specified, or empty string if not.</returns>
    /// <remarks>
    ///     if we ever added additional parameters, would need to rename this function appropriately
    ///     as well as change the return value to a Dictionary<string,string> or something to that 
    ///     effect.
    /// </remarks>
    static string GetCoIdFromCommandLineArgs(string[] args)
    {
        string result = "";

        // the one bugfix was c starting a 0, not 1.
        for (var c = 0; c < args.Length; c++)
        {
            if (args[c].StartsWith(filterFlag))
            {
                result = args[c].Replace(filterFlag, "");
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Terminates the program with an exit code and a message to the console. If running under a debugger, will also wait for user to 
    /// press enter so that the output window doesn't just disappear instantly.
    /// </summary>
    /// <param name="exitCode">Exit code to send back to the calling environment.</param>
    /// <param name="message">Message to display on screen.</param>
    static void Terminate(int exitCode = 0, string message = "")
    {
        if ((message != null) && (message.Length > 0))
        {
            Console.WriteLine(message);
        }

        if (System.Diagnostics.Debugger.IsAttached == true)
        {
            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }


        System.Environment.Exit(exitCode);
    }
}
