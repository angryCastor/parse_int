using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace parseinst
{
    class Program
    {
        static List<DataInst> dataInstList = new List<DataInst>();
        static string inputFile = "bez_botov.txt";
        static string outputFile = "result.txt";

        static void Main(string[] args)
        {

            FillUrls();
            FillJson();
            FillCountSub();
            WriteToFile();
            Console.WriteLine("Hello World!");
        }


        static void FillUrls(){
            using (StreamReader sr = new StreamReader(inputFile, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    dataInstList.Add(new DataInst{
                        Url = line
                    });
                }
            }
        }
        


        static void FillJson(){
            int current = 0;
            foreach(var item in dataInstList){
                Console.WriteLine($"{++current} || {dataInstList.Count}");
                var request = (HttpWebRequest)WebRequest.Create(item.Url);
                HttpWebResponse response = null;
                try{
                    response = (HttpWebResponse)request.GetResponseAsync().GetAwaiter().GetResult();
                }
                catch(System.Net.WebException e){
                    item.Error = "404 Not Found";
                    continue;
                }
                
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string html = null;
                        try{
                            html = GetJsonStringFromHtml(reader.ReadToEnd());
                        }
                        catch(System.IO.IOException e){
                            response.Close();
                        }
                        if(html != null){
                            item.Json = html;
                        }
                        else{
                            item.Error = "Parse error";
                        }
                    }
                }
                response.Close();
            }
        }

        static void FillCountSub(){
            foreach(var item in dataInstList){
                if(!string.IsNullOrEmpty(item.Json)){
                    int? count = GetCountSub(item.Json);
                    item.CountSub = count ?? 0;
                    item.Json = "";
                }
            }
        }


        static string GetJsonStringFromHtml(string html){
            string pattern = "<script type=\"text\\/javascript\">window\\._sharedData = ([^;]*);<\\/script>";
            var regex = new Regex(pattern);
            var match = regex.Match(html);
            
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        static int? GetCountSub(string json){
            JObject o = JObject.Parse(json);
            int? count = (int?)o.SelectToken("entry_data.ProfilePage[0].graphql.user.edge_followed_by.count");
            return count;
        }


        static void WriteToFile(){
            var list = dataInstList.Where(e => e.CountSub > 4500);
            using (StreamWriter sw = new StreamWriter(outputFile, false, System.Text.Encoding.Default))
            {
                foreach(var item in list){
                    sw.WriteLine($"{item.Url} || {item.CountSub}");
                }
            }
        }
    }


    class DataInst{
        public string Url { get; set; } = "";
        public string Json { get; set; } = "";
        public string Error { get; set; } = "";
        public int CountSub { get; set; } = 0;
    }
}
