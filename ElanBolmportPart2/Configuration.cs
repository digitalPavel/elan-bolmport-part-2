
using System.Xml.Linq;

namespace JsonParsing;

class Configuration
{
    public string SqlServer { get; private set; }

    public string SqlUser { get; private set; }

    public string SqlCatalog { get; private set; }

    public string SqlPassword
    {
        get; private set;
    }

    /// <summary>
    /// Loads the configuration from config.xml
    /// </summary>
    public Configuration()
    {
        string exeLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        XDocument configuration;
        if (File.Exists($"{exeLocation}\\config.xml") == false)
        {
            throw new FileNotFoundException("The config.xml could not be found.");
        }

        configuration = XDocument.Load($"{exeLocation}\\config.xml");
        XElement configurationGroup = configuration.Root;

        EnsureElementExists(configurationGroup, "SQL");

        XElement sqlConfig = configurationGroup.Element("SQL");
        EnsureElementExists(sqlConfig, "Server");
        EnsureElementExists(sqlConfig, "User");
        EnsureElementExists(sqlConfig, "Password");
        EnsureElementExists(sqlConfig, "Catalog");


        SqlServer = sqlConfig.Element("Server").Value;
        SqlUser = sqlConfig.Element("User").Value;
        SqlCatalog = sqlConfig.Element("Catalog").Value;
        SqlPassword = sqlConfig.Element("Password").Value;
    }

    /// <summary>
    /// Gets a true/false value from an element, default to false if the element doesn't exist.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="elementName"></param>
    /// <returns></returns>
    private bool GetBoolValueForElement(XElement collection, string elementName)
    {
        if ((collection.Element(elementName) != null) && (collection.Element(elementName).Value.ToLower() == "true"))
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    /// <summary>
    /// Validates a specific element exists in the XElement group passed.
    /// If it is missing, the program terminates immediately with a return
    /// code and human readable message.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="elementName"></param>
    public void EnsureElementExists(XElement configuration, string elementName)
    {
        if (configuration.Element(elementName) == null)
        {
            throw new FormatException($"config.xml is missing element {elementName} in configuration group.");
        }


    }
}
