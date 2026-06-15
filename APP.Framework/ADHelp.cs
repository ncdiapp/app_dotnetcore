

using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Xml;


namespace APP.Framework
{
    public class ADHelp
    {

        private static PrincipalContext _principalContext;

        private static readonly string ADServerNameP = AppConfig.Get("ADServerName") ?? string.Empty;
        private static readonly string ADUserP = AppConfig.Get("ADUser") ?? string.Empty;
        private static readonly string ADPasswordP = AppConfig.Get("ADPassword") ?? string.Empty;
        private static readonly string AuthenticationMode = (AppConfig.Get("AuthenticationMode") ?? string.Empty).ToUpperInvariant();




        //The ValidateCredentials method binds to the server specified in the constructor. If the username and password parameters are null, the credentials specified in the constructor are validated. If no credential were specified in the constructor, and the username and password parameters are null, this method validates the default credentials for the current principal
        private static PrincipalContext PrincipalContext
        {
            get
            {
                if (_principalContext == null)
                {

                    // The domain store. This represents the AD DS store.
                    //_principalContext = new PrincipalContext(ContextType.Domain, "beetle.visual2000.com");


                    if (string.IsNullOrWhiteSpace(ADUserP) || string.IsNullOrWhiteSpace(ADPasswordP))
                    {
                        _principalContext = new PrincipalContext(
                                               ContextType.Domain,
                                               ADServerNameP,
                                               null,
                                               null);
                    }
                    else
                    {

                        _principalContext = new PrincipalContext(
                                               ContextType.Domain,
                                               ADServerNameP,
                                               ADUserP,
                                               ADPasswordP);
                    }
                    


                }
                return _principalContext;

            }

        }


        public static List<string> ActUserList
        {
            get
            {
               
                    return DictSAMUserPrincipalName.Values.ToList();
             
            }


        }


        private static Dictionary<string, string> _DictSAMUserPrincipalName;
        public static Dictionary<string, string> DictSAMUserPrincipalName
        {
            get
            {
                if (_DictSAMUserPrincipalName == null)
                {
                    _DictSAMUserPrincipalName = new Dictionary<string, string>();

                    UserPrincipal userFilter = new UserPrincipal(PrincipalContext);
                    PrincipalSearcher searcher = new PrincipalSearcher(userFilter);
                    PrincipalSearchResult<Principal> users = searcher.FindAll();
                    foreach (Principal aP in users.OrderBy(ap => ap.UserPrincipalName))
                    {
                        if (!string.IsNullOrEmpty(aP.UserPrincipalName))
                        {
                            string samp = aP.SamAccountName;
                            _DictSAMUserPrincipalName.Add(samp.ToLowerInvariant(), aP.UserPrincipalName);

                        }
                        //System.Console.WriteLine(string.Format("display={0},DistinguishedName={1},Name={2}, Security Account ManagamentName(SAM){3}, UserPrincipal Name (UPN) {4} ",aP.DisplayName, aP.DistinguishedName,   aP.Name, aP.SamAccountName, aP.UserPrincipalName, aP));  


                    }

                }

                return _DictSAMUserPrincipalName;

            }


        }



        public static bool AuthenticateUser(string username, string password, out string aUserPrincipalName)
        {


            try
            {
                // bool isAuthentic = PrincipalContext.ValidateCredentials(username, password,ContextOptions.Negotiate );

                // SAM with domain prefix
                aUserPrincipalName = string.Empty;

                if (username.Contains('\\'))
                {
                    string[] split = username.Split(new char[] { '\\' });
                    if (split.Length == 2)
                    {
                        username = split[1];

                    }


                }

                bool isAuthentic = PrincipalContext.ValidateCredentials(username, password);
                if (isAuthentic)
                {
                    username = username.ToLowerInvariant();

                    if (IsUPNAccountName(username))
                    {

                        aUserPrincipalName = GetFullUPNAccountName(username); ;
                    }
                    else // SAM with domain prefix
                    {

                        if (DictSAMUserPrincipalName.ContainsKey(username))
                            aUserPrincipalName = DictSAMUserPrincipalName[username];

                    }


                    return true;
                }
                else
                {

                    return false;

                }






            }
            catch
            {
                aUserPrincipalName = string.Empty;
                return false;

            }
        }


        private static bool IsUPNAccountName(string username)
        {
            return username.Contains("@");

        }

        private static string GetFullUPNAccountName(string upnName)
        {
            foreach (var upn in DictSAMUserPrincipalName.Values)
            {
                if (upn.ToLowerInvariant().Contains(upnName))
                    return upn;

            }

            return string.Empty; ;


        }


        private static Dictionary<string, PrincipalContext> domainContexts = null;


        private static Dictionary<string, PrincipalContext> DomainContexts
        {
            get
            {
                if (domainContexts == null)
                {
                    domainContexts = new Dictionary<string, PrincipalContext>();
                    DirectoryEntry rootDSE = new DirectoryEntry("GC://RootDSE");
                    string rootDomain = "GC://" + rootDSE.Properties["rootDomainNamingContext"].Value.ToString();


                    DirectoryEntry searchRoot = new DirectoryEntry(rootDomain);
                    string[] propsToLoad = { "name", "distinguishedName" };
                    DirectorySearcher domainSearcher = new DirectorySearcher(searchRoot, "objectCategory=domainDNS", propsToLoad, System.DirectoryServices.SearchScope.Subtree);

                    //DirectorySearcher domainSearcher = new DirectorySearcher(searchRoot, "objectCategory=visual2000.com", propsToLoad,  SearchScope.Subtree);


                    SearchResultCollection domainCollection = domainSearcher.FindAll();
                    foreach (SearchResult domain in domainCollection)
                        domainContexts.Add(domain.Properties["name"][0].ToString(),
                                        new PrincipalContext(ContextType.Domain, null,
                        domain.Properties["distinguishedName"][0].ToString()));
                }
                return domainContexts;
            }



        }

    }
    public class LDAPHelp
    {


        //  When connecting to AD using the .NET Framework,
        //  you can use "serverless" binding 
        //or you can specify a server to use everytime (server bound).


        //serverless
        DirectoryEntry rootConfig = new DirectoryEntry("LDAP://dc=domainname,dc=com");
        //where "dc" denotes 'Domain Component'.
        // server bound 
        DirectoryEntry rootEntry = new DirectoryEntry("LDAP://domainControllerName/dc=domainName,dc=com");

        void setup()
        {
            DirectoryEntry localMachine = new DirectoryEntry
        ("WinNT://" + Environment.MachineName + ",Computer");

            DirectoryEntry admGroup = localMachine.Children.Find
                ("Administrators", "group");

            object members = admGroup.Invoke("members", null);

            foreach (object groupMember in (IEnumerable)members)
            {
                DirectoryEntry member = new DirectoryEntry(groupMember);
                //output.RenderBeginTag("p");
                //output.Write(member.Name.ToString());
                //output.RenderBeginTag("p");
            }
        }



        //        DirectoryEntry deRoot = new DirectoryEntry("LDAP://RootDSE"); 

        //if (deRoot != null) 
        //{ 
        //  Console.WriteLine("Default naming context: " + deRoot.Properties["defaultNamingContext"].Value); 
        //  Console.WriteLine("Server name: " + deRoot.Properties["serverName"].Value); 
        //  Console.WriteLine("DNS host name: " + deRoot.Properties["dnsHostName"].Value); 

        //  Console.WriteLine(); 
        //  Console.WriteLine("Additional properties:"); 
        //  foreach (string propName in deRoot.Properties.PropertyNames) 
        //    Console.Write(propName + ", "); 
        //  Console.WriteLine(); 
        //} 


        //        ldap://host:port/DN?attributes?scope?filter?extensions
        //Most of the components, which are described below, are optional.

        //host is the FQDN or IP address of the LDAP server to search. 
        //port is the network port of the LDAP server. 
        //DN is the distinguished name to use as the search base. 
        //attributes is a comma-separated list of attributes to retrieve. 
        //scope specifies the search scope and can be "base" (the default), "one" or "sub". 
        //filter is a search filter. For example (objectClass=*) as defined in RFC 4515. 
        //extensions are extensions to the LDAP URL format. 
        //For example, "ldap://ldap.example.com/cn=John%20Doe,dc=example,dc=com" refers to all user attributes in John Doe's entry in ldap.example.com, while "ldap:///dc=example,dc=com??sub?(givenName=John)" searches for the entry in the default server (note the triple slash, omitting the host, and the double question mark, omitting the attributes). As in other URLs, special characters must be percent-encoded.

        //There is a similar non-standard ldaps: URL scheme for LDAP over SSL. This should not be confused with LDAP with TLS, which is achieved using the StartTLS operation using the standard ldap: scheme


    }


    public class LDAPComLink
    {
        public LDAPComLink()
        {

        }

        public XmlDocument GetLDAPInfo(string filter)
        {
            XmlDocument xd = new XmlDocument();
            string domainAndUsername = string.Empty;
            string userName = string.Empty;
            string passWord = string.Empty;
            AuthenticationTypes at = AuthenticationTypes.Anonymous;
            StringBuilder sb = new StringBuilder();

            //****Connecting to LDAP active directory
            domainAndUsername = @"LDAP://servername:port/cn=Users,dc=ldaptest";
            userName = "Kenno";
            passWord = "password";
            at = AuthenticationTypes.Secure;

            //Create the object necessary to read the info from the LDAP directory
            DirectoryEntry entry = new DirectoryEntry(domainAndUsername, userName, passWord, at);
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            SearchResultCollection results;
            mySearcher.Filter = filter;

            try
            {
                results = mySearcher.FindAll();

                if (results.Count > 0)
                {
                    sb.Append("<Users>");
                    foreach (SearchResult resEnt in results)
                    {
                        sb.Append("<User>");
                        ResultPropertyCollection propcoll = resEnt.Properties;
                        foreach (string key in propcoll.PropertyNames)
                        {
                            foreach (object values in propcoll[key])
                            {
                                switch (key)
                                {
                                    case "sn":
                                        sb.Append("<surname>" + values.ToString() + "</surname>");
                                        break;
                                    case "cn":
                                        sb.Append("<cn>" + values.ToString() + "</cn>");
                                        break;
                                    case "name":
                                        sb.Append("<name>" + values.ToString() + "</name>");
                                        break;
                                    case "givenname":
                                        sb.Append("<givenname>" + values.ToString() + "</givenname>");
                                        break;
                                    case "distinguishedname":
                                        sb.Append("<distinguishedname>" + values.ToString() + "</distinguishedname>");
                                        break;
                                    case "member":
                                        sb.Append("<member>" + values.ToString() + "</member>");
                                        break;
                                    case "initials":
                                        sb.Append("<initials>" + values.ToString() + "</initials>");
                                        break;
                                    case "postalcode":
                                        sb.Append("<postalcode>" + values.ToString() + "</postalcode>");
                                        break;
                                    case "l":
                                        sb.Append("<location>" + values.ToString() + "</location>");
                                        break;
                                    case "c":
                                        sb.Append("<c>" + values.ToString() + "</c>");
                                        break;
                                    case "mobile":
                                        sb.Append("<mobile>" + values.ToString() + "</mobile>");
                                        break;
                                    case "homephone":
                                        sb.Append("<homephone>" + values.ToString() + "</homephone>");
                                        break;
                                    case "title":
                                        sb.Append("<title>" + values.ToString() + "</title>");
                                        break;
                                    case "co":
                                        sb.Append("<co>" + values.ToString() + "</co>");
                                        break;
                                    case "st":
                                        sb.Append("<state>" + values.ToString() + "</state>");
                                        break;
                                    case "mail":
                                        sb.Append("<mail>" + values.ToString() + "</mail>");
                                        break;
                                    case "password":
                                        sb.Append("<password>" + values.ToString() + "</password>");
                                        break;
                                    case "samaccountname":
                                        sb.Append("<samaccountname>" + values.ToString() + "</samaccountname>");
                                        break;
                                    case "memberof":
                                        sb.Append("<memberof>" + values.ToString() + "</memberof>");
                                        break;
                                    case "uid":
                                        sb.Append("<userid>" + values.ToString() + "</userid>");
                                        break;
                                    case "description":
                                        sb.Append("<description>" + values.ToString() + "</description>");
                                        break;
                                }
                            }
                        }
                        sb.Append("</User>");
                    }
                    sb.Append("</Users>");
                    xd.LoadXml(sb.ToString());
                    return xd;
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
            sb.Append("<Users><User>None</User></Users>");
            xd.LoadXml(sb.ToString());
            return xd;
        }
    }

    //Lightweight Directory Access Protocol
#if NETFRAMEWORK
    [DirectoryServicesPermission(SecurityAction.Demand, Unrestricted = true)]
#endif
    public class LDAPConnect
    {
        // static variables used throughout the example
        static LdapConnection ldapConnection;
        static string ldapServer;
        static NetworkCredential credential;
        static string targetOU; // dn of an OU. eg: "OU=sample,DC=fabrikam,DC=com"

        public static void Main(string[] args)
        {
            try
            {
                GetParameters(args);  // Get the Command Line parameters

                // Create the new LDAP connection
                ldapConnection = new LdapConnection(ldapServer);
                ldapConnection.Credential = credential;
                Console.WriteLine("LdapConnection is created successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nUnexpected exception occurred:\r\n\t" + e.GetType() + ":" + e.Message);
            }
        }

        static void GetParameters(string[] args)
        {
            // When running: ConnectLDAP.exe <ldapServer> <user> <pwd> <domain> <targetOU>

            if (args.Length != 5)
            {
                Console.WriteLine("Usage: ConnectLDAP.exe <ldapServer> <user> <pwd> <domain> <targetOU>");
                Environment.Exit(-1);// return an error code of -1
            }

            // test arguments to ensure they are valid and secure

            // initialize variables
            ldapServer = args[0];
            credential = new NetworkCredential(args[1], args[2], args[3]);
            targetOU = args[4];
        }
    }

    //    LDAP Authentication and Single Sign On

    // Single Sign On (SSO) systems mostly use LDAP authentication. The enterprise user logs on in the morning and sees normally a form based enterprise login screen. 
    //    The user enters in their id and password. The SSO software then takes the information and sends it to the security server using an encrypted connection. 
    // The security server in turn then logs on to the LDAP server on behalf of the user by providing the LDAP server with the user's id and password.
    //   If successful, the security server then proceeds with any authorization and/or lets the user proceed to the application or resource they require.


    // The Lightweight Directory Access Protocol (LDAP; /ˈɛldæp/) is an application protocol for accessing and maintaining distributed directory information services over an Internet Protocol (IP) network.[1]

    //Directory services may provide any organized set of records, often with a hierarchical structure, such as a corporate email directory. Similarly, a telephone directory is a list of subscribers with an address and a phone number.

    //LDAP is specified in a series of Internet Engineering Task Force (IETF) Standard Track publications called Request for Comments (RFCs), using the description language ASN.1. The latest specification is Version 3, published as RFC 4511. For example, here is an LDAP search translated into plain English: "Search in the company email directory for all people located in Boston whose name contains 'Jesse' that have an email address. Please return their full name, email, title, and description."[2]

    //A common usage of LDAP is to provide a "single sign-on" where one password for a user is shared between many services, such as applying a company login code to web pages (so that staff log in only once to company computers, and then are automatically logged into the company intranet).[2]



}
