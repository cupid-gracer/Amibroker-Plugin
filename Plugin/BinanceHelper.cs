using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using System.Windows.Controls;
using System.Windows.Forms;
// JSON
using Newtonsoft.Json;
using WebSocket4Net;

using AmiBroker.Plugin.Models;

namespace AmiBroker.Plugin
{
    class BinanceHelper
    {
        // Адрес, по которому запрашиваются все торгуемые пары
        //public static string PAIRS_URL = "https://www.binance.com/exchange/public/product";
        public static string PAIRS_URL = "https://api.binance.com/api/v3/exchangeInfo";
        
        //public static string PAIRS_URL = "http://kb2web/data.php";

        // Шаблон адреса получения котировок за последние 24 часа
        //public static string LAST24_BARS = "https://www.binance.com/api/v1/klines?symbol={0}&interval={1}&limit=1000";
        public static string LAST24_BARS = "https://api.binance.com/api/v3/klines?symbol={0}&interval={1}&limit=1000";
        public static string LAST24_BARSFROMTO = "https://api.binance.com/api/v3/klines?symbol={0}&interval={1}&limit=1000&startTime={2}&endTime={3}";
        public static string TICKER24HR = "https://api.binance.com/api/v3/ticker/24hr?symbol={0}";
        //public static string LAST24_BARS = "http://kb2web/1min.php?{0}&{1}";

        // WebSocketStream
        public static string WSS_URL = "wss://stream.binance.com:9443/ws/{0}@kline_{1}";
        public static string WSS_24hrURL = "wss://stream.binance.com:9443/ws/!ticker@arr";

        public static WebSocket wsSocket = null;
        public static WebSocket ws24hrSocket = null;

        public static EventHandler<MessageReceivedEventArgs> onWSSMessage;
        public static EventHandler<MessageReceivedEventArgs> onWSS24hrMessage;

        public static bool CreateWSS(string pairName, Periodicity period)
        {
            bool result = false;
            string url;

            // Закрыть
            if (wsSocket != null)
            {
                wsSocket.Close();
                wsSocket.Dispose();
            }

            // Период
            string interval = "1m";
            switch (period)
            {
                case Periodicity.EndOfDay: interval = "1d"; break;
                case Periodicity.FifteenMinutes: interval = "15m"; break;
                case Periodicity.FiveMinutes: interval = "5m"; break;
                case Periodicity.OneHour: interval = "1h"; break;
                case Periodicity.OneMinute: interval = "1m"; break;

            }
            url = String.Format(WSS_URL, pairName.ToLower(), interval);               

            // Открываем сокет
            try
            {
                wsSocket = new WebSocket(url);
                wsSocket.MessageReceived += onMessage;
                wsSocket.Opened += wsSocket_Opened;
                wsSocket.Open();

                result = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't create web socket stream! Error: " + e.Message, "Info", MessageBoxButtons.OK);

                Log.Write("Can't create web socket stream! Error: " + e.Message);
                    return false;
            }

            return result;
        }


        public static bool Create24hrWSS()
        {
            bool result = false;

            // Закрыть
            if (ws24hrSocket != null)
            {
                return true;
                //ws24hrSocket.Close();
            }

            // Открываем сокет
            try
            {
                ws24hrSocket = new WebSocket(WSS_24hrURL);
                ws24hrSocket.MessageReceived += on24hrMessage;
                ws24hrSocket.Opened += wsSocket_Opened;
                ws24hrSocket.Open();

                result = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't create web 24hr socket stream! Error: " + e.Message, "Info", MessageBoxButtons.OK);

                Log.Write("Can't create web socket stream! Error: " + e.Message);
                return false;
            }

            return result;
        }

        private static void onMessage(object sender, MessageReceivedEventArgs e)
        {
            onWSSMessage(sender, e);
        }

        private static void on24hrMessage(object sender, MessageReceivedEventArgs e)
        {
            onWSS24hrMessage(sender, e);
        }

        static void wsSocket_Opened(object sender, EventArgs e)
        {
            Log.Write("Socket connecting...");
        }



        // Получение котировок за последние 24 часа
        public static List<Ticker> getLast24hBars(string pairName, Periodicity periodicity)
        {
            List<Ticker> result = new List<Ticker>();
            string interval = "1m";
            switch (periodicity)
            {
                case Periodicity.EndOfDay: interval = "1d"; break;
                case Periodicity.FiveMinutes: interval = "5m"; break;
                case Periodicity.FifteenMinutes: interval = "15m"; break;
                case Periodicity.OneHour: interval = "1h"; break;
                case Periodicity.OneMinute: interval = "1m"; break;
            }

            if (interval.Equals("1d"))
            {
                //2016-01-01 ~ 2018-12-31
                getLastBarsFromTo(ref result, pairName, interval, "1451606400134", "1546214400000");
                //2019-01-01 ~ 2021-12-31
                getLastBarsFromTo(ref result, pairName, interval, "1546214500000", "1640995200000");
            }
            else
            {
                getLastBarsFromTo(ref result, pairName, interval);
            }
            // Возвращаем массив
            return result;
        }

        private static void getLastBarsFromTo(ref List<Ticker> result, string pairName, string interval, string from = "", string to = "")
        {
            // Строка запроса - данные за последние 24 часа в 1 мин интервале
            string url;
            if(interval != "1d")
            {
                url = String.Format(LAST24_BARS, pairName, interval);
            }
            else
            {
                url = String.Format(LAST24_BARSFROMTO, pairName, interval, from, to);
            }
            // Получаем ответ
            string answer = getJSONData(url);

            //Log.Write(answer, "answer_" + pairName + ".log");
            // Массив данных
            List<ArrayList> decodedList = null;

            try
            {
                decodedList = JsonConvert.DeserializeObject<List<ArrayList>>(answer);
            }
            catch (Exception e)
            {
                Log.Write("Can't parse JSON data for last 24H bars!");
                return ;
            }

            if (decodedList.Count == 0)
            {
                Log.Write("Decooded JSON data for last 24H bars is empty!");
                return ;
            }

            int index = 0;

            foreach (ArrayList arr in decodedList)
            {
                Ticker ticker = new Ticker();

                // Формирование даты
                AmiDate time = new AmiDate(Utils.UnixTimeStampToDateTime(Convert.ToUInt64(arr[0]) / 1000));

                // Bar
                ticker.time = time.ToUInt64();
                //ticker.open = float.Parse(arr[1].ToString().Replace(".", ","));
                ticker.open = float.Parse(arr[1].ToString());
                ticker.high = float.Parse(arr[2].ToString());
                ticker.low = float.Parse(arr[3].ToString());
                ticker.close = float.Parse(arr[4].ToString());
                ticker.volume = float.Parse(arr[5].ToString());

                // Запись в массив
                result.Add(ticker);
            }
        }


        public static Ticker24hr getTicker24hr(string pairName)
        {
            Ticker24hr result = new Ticker24hr();
            string url = String.Format(TICKER24HR, pairName);
            string answer = getJSONData(url);

            result = JsonConvert.DeserializeObject<Ticker24hr>(answer);
            return result;
        }
        // Получение торгуемых символьных пар на Binance
        public static List<SymbolInfo> getAllPairs()
        {
            PairInfo pairList = null;            

            string answer = getJSONData(PAIRS_URL);

            try
            {
                pairList = JsonConvert.DeserializeObject<PairInfo>(answer);
            }
            catch (Exception e)
            {
                Log.Write("Can't parse JSON data! Message: " + e.Message);
                return null;
            }

            List<SymbolInfo> infoList = new List<SymbolInfo>();

            foreach(Pair item in pairList.symbols)
            {
                SymbolInfo info = new SymbolInfo();

                info.pairName = item.symbol;
                info.baseSymbol = item.baseAsset;
                info.quoteSymbol = item.quoteAsset;
                info.description = item.baseAsset + "/" + item.quoteAsset + " at Binance Exchange";

                infoList.Add(info);
            }

            return infoList;
        }


        public static string getJSONData(string baseURL)
        {
            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            string result = "";

            // Пробуем запрос данных
            HttpWebRequest httpRequest;
            HttpWebResponse httpResponse;


            // Создать запрос
            httpRequest = (HttpWebRequest)WebRequest.Create(baseURL);

            try
            {
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                // Ссылка на поток
                Stream webStream = httpResponse.GetResponseStream();
                StreamReader reader = new StreamReader(webStream);

                // Наш ответ с сервера
                result = reader.ReadToEnd();

                // Закрываем все
                reader.Close();
                webStream.Close();
                httpResponse.Close();
            }
            catch (Exception e)
            {
                Log.Write("GetJSONData Error: " + e.Message);
                return null;
            }

            return result;
        }

    }
}
