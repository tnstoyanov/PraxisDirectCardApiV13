﻿//////////////////////////////////////////////////////////////////////////////////
// This will generate an HTTPS request based on Praxis's Direct Card API, v1.3  //
// This is a REST-ful API                                                       //
// Demo endpoint: https://pci-gw-test.praxispay.com/api/direct-process          //
// Live endpoint: https://gw.praxisgate.com/api/direct-process                  //
//////////////////////////////////////////////////////////////////////////////////

using System.Security.Cryptography;
using System.Text;

// API user. Get it from Praxis. Variables, do NOT hardcode!
//  Invesus test API user
string merchant_id = "API-Invesus";
string application_key = "Invesus.com";
string merchant_secret = "u66gDFAz1zhIsjGhA9mrvzWa7WGWFYRm";
// End of Invesus test API user

/* WBS LIVE creds, you should hit Praxis' live environment, see above */
/*
string merchant_id = "API-wbandsmith";
string application_key = "wbandsmith.com";
string merchant_secret = "kAbFPsWuiUXGy58s1k30gpa5TD6jCky2";
*/
/* End of WBandSmith creds */

/* Zenstox LIVE creds, you should hit Praxis' live environment, see above */
/*
string merchant_id = "API-zenstox";
string application_key = "zenstox.com";
string merchant_secret = "yw03C0qe1KpuviiWKYz2sIBeHCpXB54J";
*/
/* End of WBandSmith creds */


// This will create a Sale transaction
string transaction_type = "sale";

// Request parameters
// USD is in cents, so 10000 = 100.00 USD. Stick to Praxis' currency list: https://doc.praxiscashier.com/integration_docs/latest/overview/data_formats
/*
string currency = "USD";
string amount = "10000";
*/
// JPY and other exotic currencies with higher inflation rates are NOT a fraction of 1 e.g., 10000 units = 10000 JPY (cca 65 USD in Jan 2025). See the exotic list: https://doc.praxiscashier.com/integration_docs/latest/overview/data_formats
string currency = "JPY";
// Always process JPY as it is, so 10000 = 10000 JPY. Do NOT calculate like you do the USD, EUR, etc.
string amount = "10000";

// Get Unix time, seconds
// UTC = GMT
DateTime current_time = DateTime.UtcNow;
long timestamp = ((DateTimeOffset)current_time).ToUnixTimeSeconds();

/*** encrypt card_data object ***/

// Invoke AES-256-CBC encryption
static byte[] Encrypt(string simpletext, byte[] key, byte[] iv)
{
    byte[] cipheredtext;
    using (Aes aes = Aes.Create())
    {
        ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(simpletext);
                }

                cipheredtext = memoryStream.ToArray();
            }
        }
    }
    return cipheredtext;
}

// Raw card details. Use AES-256-CBC for the encryption. 568 is for 3NDS and 333 is for 3DS
string card_number = "5176636352156580";
string card_exp = "04/2027";
// 333 will trigger 3DS
string cvv = "333";
// 568 will trigger N3DS
//string cvv = "568";
// will trigger a failure (insufficient funds)
// string cvv = "801";

// Get the AES key (the merchant_secret) and iv (the timestamp)
byte[] secretKey = Encoding.ASCII.GetBytes(merchant_secret.PadLeft(32, '0'));
byte[] requestTimestamp = Encoding.ASCII.GetBytes(timestamp.ToString().PadLeft(16, '0'));

// Encrypt the card_number
byte[] encryptedCard = Encrypt(card_number, secretKey, requestTimestamp);
string encryptedCardString = Convert.ToBase64String(encryptedCard);

// Encrypt the card_exp
byte[] encryptedExp = Encrypt(card_exp, secretKey, requestTimestamp);
string encryptedExpString = Convert.ToBase64String(encryptedExp);

// Encrypted the cvv
byte[] encryptedCvv = Encrypt(cvv, secretKey, requestTimestamp);
string encryptedCvvString = Convert.ToBase64String(encryptedCvv);
/*** End of card_data encryption ***/

// device_data object (browser info)
string user_agent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Safari/605.1.15";
string accept_header = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
string language = "en-us";
string ip_address = "87.120.109.134";
int timezone_offset = 180;
string color_depth = "24";
string pixel_depth = "24";
string pixel_ratio = "2";
int screen_height = 900;
int screen_width = 1440;
int viewport_height = 400;
int viewport_width = 1440;
int java_enabled = 0;
int javascript_enabled = 1;
// PROfit data
Random rnd = new Random();
// TradeNetworks.dbo.Users.UserId
int cid = rnd.Next();
string locale = "en-GB";
// customer_data object
string country = "JP";
string first_name = "Musashibo";
string last_name = "Benkei";
string dob = "01/01/1978";
string email = "test.jpy.mc@qq.com";
string phone = "359888123456";
string zip = "10010";
string city = "Sapporo";
string address = "Keiko Juku 12";
int profile = 0;
// This routes your transaction to the relevant Payment Solution Provider (PSP)
// Subaccounbt in PBO CC GW accounts!!!
// take the hash from Praxis' back office from the Gateways view 
string gateway = "040e154f306f145b84208512d00ef8d9";
// Callback part
string notification_url = "https://165191ec2e6bda1c110b03cd4e4f9e79.m.pipedream.net";
// 3DS Return URL to Deposit Site
string return_url = "https://tnstoyanov.wixsite.com/payment-response/return/?get=1";
int order_id = rnd.Next();
string version = "1.3";

// Signature part, goes into the HTTP headers. We should use SHA384
string InputBytes =
    merchant_id
    + application_key
    + timestamp
    + transaction_type
    + cid
    + order_id
    + currency
    + amount
    + gateway
    + notification_url
    + return_url
    + encryptedCardString
    + merchant_secret;

using (SHA384 sha384Hash = SHA384.Create())
{
    //From String to byte array
    byte[] sourceBytes = Encoding.UTF8.GetBytes(InputBytes);
    byte[] hashBytes = sha384Hash.ComputeHash(sourceBytes);

    // replacing "-" with empty
    string signature = BitConverter.ToString(hashBytes).Replace("-", String.Empty).ToLower();
    Console.WriteLine("/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
    Console.WriteLine("// Add this signature to your HTTP headers, for the GT-Authentication parameter:                                   //");
    Console.WriteLine("//                                                                                                                 //");
    Console.WriteLine("// " + signature.PadRight(111, ' ') + " //");
    Console.WriteLine("/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
    Console.WriteLine();
}

// This builds the HTTPS request payload
Console.WriteLine("{");
Console.WriteLine("\"merchant_id\": \"{0}\",", merchant_id);
Console.WriteLine("\"application_key\": \"{0}\",", application_key);
Console.WriteLine("\"transaction_type\": \"{0}\",", transaction_type);
Console.WriteLine("\"currency\": \"{0}\",", currency);
Console.WriteLine("\"amount\": {0},", amount);
Console.WriteLine("\"card_data\": {");
Console.WriteLine("\"card_number\": \"{0}\",", encryptedCardString);
Console.WriteLine("\"card_exp\": \"{0}\",", encryptedExpString);
Console.WriteLine("\"cvv\": \"{0}\"", encryptedCvvString);
Console.WriteLine("},");
Console.WriteLine("\"device_data\": {");
Console.WriteLine("\"user_agent\": \"{0}\",", user_agent);
Console.WriteLine("\"accept_header\": \"{0}\",", accept_header);
Console.WriteLine("\"language\": \"{0}\",", language);
Console.WriteLine("\"ip_address\": \"{0}\",", ip_address);
Console.WriteLine("\"timezone_offset\": {0},", timezone_offset);
Console.WriteLine("\"color_depth\": \"{0}\",", color_depth);
Console.WriteLine("\"pixel_depth\": \"{0}\",", pixel_depth);
Console.WriteLine("\"pixel_ratio\": \"{0}\",", pixel_ratio);
Console.WriteLine("\"screen_height\": {0},", screen_height);
Console.WriteLine("\"screen_width\": {0},", screen_width);
Console.WriteLine("\"viewport_height\": {0},", viewport_height);
Console.WriteLine("\"viewport_width\": {0},", viewport_width);
Console.WriteLine("\"java_enabled\": {0},", java_enabled);
Console.WriteLine("\"javascript_enabled\": {0}", javascript_enabled);
Console.WriteLine("},");
Console.WriteLine("\"cid\": \"{0}\",", cid);
Console.WriteLine("\"locale\": \"{0}\",", locale);
Console.WriteLine("\"customer_data\": {");
Console.WriteLine("\"country\": \"{0}\",", country);
Console.WriteLine("\"first_name\": \"{0}\",", first_name);
Console.WriteLine("\"last_name\": \"{0}\",", last_name);
Console.WriteLine("\"dob\": \"{0}\",", dob);
Console.WriteLine("\"email\": \"{0}\",", email);
Console.WriteLine("\"phone\": \"{0}\",", phone);
Console.WriteLine("\"zip\": \"{0}\",", zip);
Console.WriteLine("\"city\": \"{0}\",", city);
Console.WriteLine("\"address\": \"{0}\",", address);
Console.WriteLine("\"profile\": \"{0}\"", profile);
Console.WriteLine("},");
Console.WriteLine("\"gateway\": \"{0}\",", gateway);
Console.WriteLine("\"notification_url\": \"{0}\",", notification_url);
Console.WriteLine("\"return_url\": \"{0}\",", return_url);
Console.WriteLine("\"order_id\": \"{0}\",", order_id);
Console.WriteLine("\"version\": \"{0}\",", version);
Console.WriteLine("\"timestamp\": {0}", timestamp);
Console.WriteLine("}");

// This keeps your console window open
Console.ReadLine();