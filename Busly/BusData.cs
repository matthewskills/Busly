using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data.SQLite;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Xml;
using System.IO.Compression;
using System.IO;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;

namespace Busly
{
    public class BusData
    {
        private static string NextBusURI = String.Empty;
        private static string NextBusCredentials = String.Empty;
        private static string DfTBusDataURI = String.Empty;
        private static string DfTBusDataAPIKey = String.Empty;

        private static int NextBusAPIHits = 0;


        public static void setOptions(string optNextBusURI, string optNextBusCredentials, string optDfTBusDataURI, string optDfTBusDataAPIKey) {
            NextBusURI = optNextBusURI;
            NextBusCredentials = optNextBusCredentials;
            DfTBusDataURI = optDfTBusDataURI;
            DfTBusDataAPIKey = optDfTBusDataAPIKey;
        }

        enum eNaptanStop {
            ATCOCode = 0,
            CommonName = 4,
            Street = 10,
            LocalityName = 18,
            ParentLocality = 19,
            Longitude = 29,
            Latitude = 30
        }

        public class NaptanStop {
            public string? ATCOCode { get; set; }
            public string? CommonName { get; set; }
            public string? Street { get; set; }
            public string? LocalityName { get; set; }
            public string? ParentLocality { get; set; }
            public double? Longitude { get; set; }
            public double? Latitude { get; set; }
        }


        public static string GetStops(Double lat, Double lng)
        {

            double meters = 1609;
            double earth = 6378.137; //radius of the earth in kilometer
            double m = (1 / ((2 * Math.PI / 360) * earth)) / 1000;  //1 meter in degree

            double new_lat = lat + (meters * m);
            double new_long = lng + (meters * m) / Math.Cos(lat * (Math.PI / 180));
            double new_lat2 = lat - (meters * m);
            double new_long2 = lng - (meters * m) / Math.Cos(lat * (Math.PI / 180));


            SQLiteDataReader result = null;
            string command = $"SELECT * FROM naptan WHERE (Latitude BETWEEN {new_lat2} AND {new_lat}) AND (Longitude BETWEEN {new_long2} AND {new_long}) AND BusStopType IS NOT NULL LIMIT 500";
            SQLiteConnection sqlite_conn;
            sqlite_conn = SQL.CreateConnection();

            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = command;

            sqlite_datareader = sqlite_cmd.ExecuteReader();
            result = sqlite_datareader;

           
            List<NaptanStop> NaptanStoplist = new List<NaptanStop>();

            while (result.Read()) {

                NaptanStoplist.Add(new NaptanStop()
                {
                    ATCOCode = result.GetValue((int)eNaptanStop.ATCOCode) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.ATCOCode),
                    CommonName = result.GetValue((int)eNaptanStop.CommonName) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.CommonName),
                    Street = result.GetValue((int)eNaptanStop.Street) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.Street),
                    LocalityName = result.GetValue((int)eNaptanStop.LocalityName) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.LocalityName),
                    ParentLocality = result.GetValue((int)eNaptanStop.ParentLocality) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.ParentLocality),
                    Longitude = result.GetValue((int)eNaptanStop.Longitude) == DBNull.Value ? 0.0 : (double?)result.GetValue((int)eNaptanStop.Longitude),
                    Latitude = result.GetValue((int)eNaptanStop.Latitude) == DBNull.Value ? 0.0 : (double?)result.GetValue((int)eNaptanStop.Latitude)
                });

            }

            sqlite_conn.Close();

            var json = System.Text.Json.JsonSerializer.Serialize(NaptanStoplist);

            return json.ToString();
        }

        public static string StopSearch(String query)
        {

            SQLiteDataReader result = null;
            string command = $"SELECT * FROM naptan WHERE (CommonName LIKE '%{query}%' OR Street LIKE '%{query}%' Or LocalityName LIKE '%{query}%'  OR ParentLocalityName Like '%{query}%' ) AND BusStopType IS NOT NULL LIMIT 500";
            SQLiteConnection sqlite_conn;
            sqlite_conn = SQL.CreateConnection();

            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = command;

            sqlite_datareader = sqlite_cmd.ExecuteReader();
            result = sqlite_datareader;


            List<NaptanStop> NaptanStoplist = new List<NaptanStop>();

            while (result.Read())
            {

                NaptanStoplist.Add(new NaptanStop()
                {
                    ATCOCode = result.GetValue((int)eNaptanStop.ATCOCode) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.ATCOCode),
                    CommonName = result.GetValue((int)eNaptanStop.CommonName) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.CommonName),
                    Street = result.GetValue((int)eNaptanStop.Street) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.Street),
                    LocalityName = result.GetValue((int)eNaptanStop.LocalityName) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.LocalityName),
                    ParentLocality = result.GetValue((int)eNaptanStop.ParentLocality) == DBNull.Value ? String.Empty : (string?)result.GetValue((int)eNaptanStop.ParentLocality),
                    Longitude = result.GetValue((int)eNaptanStop.Longitude) == DBNull.Value ? 0.0 : (double?)result.GetValue((int)eNaptanStop.Longitude),
                    Latitude = result.GetValue((int)eNaptanStop.Latitude) == DBNull.Value ? 0.0 : (double?)result.GetValue((int)eNaptanStop.Latitude)
                });

            }

            sqlite_conn.Close();

            var json = System.Text.Json.JsonSerializer.Serialize(NaptanStoplist);

            return json.ToString();
        }



        public static string GetStopData(String atcocode)  {

            if (NextBusAPIHits >= 1000) {
                return string.Empty;
            }

            string siriXML = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?> \r\n<Siri version=\"1.0\" xmlns=\"http://www.siri.org.uk/\"> \r\n <ServiceRequest> \r\n <RequestTimestamp>{DateTime.Now.ToString("yyyyMMddHHmmss")}</RequestTimestamp> \r\n <RequestorRef>TravelineAPI101</RequestorRef> \r\n <StopMonitoringRequest version=\"1.0\"> \r\n <RequestTimestamp>2014-07-01T15:09:12Z</RequestTimestamp>  <MessageIdentifier>12345</MessageIdentifier> \r\n <MonitoringRef>{atcocode}</MonitoringRef> \r\n </StopMonitoringRequest> \r\n </ServiceRequest> \r\n</Siri>";
            HttpContent content = new StringContent(siriXML, Encoding.UTF8, "application/xml");


            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(NextBusURI)

            };

            NextBusAPIHits += 1;

            string authenticationString = NextBusCredentials;
            string base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            using (var response = httpClient.PostAsync("",content))
            {

                string responseBody = response.Result.Content.ReadAsStringAsync().Result;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseBody);
                string json = JsonConvert.SerializeXmlNode(doc);


                return json.ToString();
            }

        }

        public static string GetVehicleTrackingData(String lineRef, String operatorRef)
        {

            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = new Uri($"{DfTBusDataURI}/datafeed/?operatorRef={operatorRef}&lineRef={lineRef}&api_key={DfTBusDataAPIKey}")
                };

            using (var response = httpClient.GetAsync(""))
            {

                string responseBody = response.Result.Content.ReadAsStringAsync().Result;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseBody);
                string json = JsonConvert.SerializeXmlNode(doc);

                return json.ToString();
            }

        }


        public static string GetRouteData(String noc, String search)
        {
            search = search.Substring(0, 3);

            string timetableUrl = String.Empty;

            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri($"{DfTBusDataURI}/dataset/?adminArea={search}&noc={noc}&limit=1&offset=0&status=published&api_key={DfTBusDataAPIKey}")
            };

            using (var response = httpClient.GetAsync(""))
            {

                string responseBody = response.Result.Content.ReadAsStringAsync().Result;

                timetableUrl = responseBody.Split("\"url\":\"")[1].Split("\"")[0];
       

                
            }

            httpClient.Dispose();

            httpClientHandler = new HttpClientHandler();
            httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(timetableUrl)
            };

            using (var response = httpClient.GetStreamAsync(""))
            {

                string extractPath = AppDomain.CurrentDomain.BaseDirectory + $"\\downloads\\{search}_{noc}\\";
                string downloadPath = AppDomain.CurrentDomain.BaseDirectory + $"\\downloads\\{search}_{noc}.zip";

                using (Stream zip = File.OpenWrite(downloadPath))
                {
                    response.Result.CopyTo(zip);
                }

                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(downloadPath, extractPath);

                string[] fileEntries = Directory.GetFiles(extractPath);
                foreach (string fileName in fileEntries)
                {

                    using (var streamReader = new StreamReader(@""+fileName, Encoding.UTF8))
                    {
                        string text = streamReader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(text);
                        string json = JsonConvert.SerializeXmlNode(doc);
                        Console.WriteLine(json);
                    }

                   
                }

            }

            return String.Empty;

        }

    }
}
