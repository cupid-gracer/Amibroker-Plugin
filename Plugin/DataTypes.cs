using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Plugin
{

    // Данные графика
    public class Ticker
    {
        public ulong time { get; set; }
        public float close { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float open { get; set; }
        public float volume { get; set; }
    }

    class SymbolInfo
    {
        public string pairName { get; set; }
        public string baseSymbol { get; set; }
        public string quoteSymbol { get; set; }
        public string description { get; set; }
    }

    // Запрос пар идет в виде объекта в котором определен массив 
    // пар Pair
    public class PairInfo
    {
        public List<Pair> symbols { get; set; }
    }

    public class Pair
    {
        public string symbol { get; set; }
        public string baseAsset { get; set; }
        public int baseAssetPrecision { get; set; }
        public int baseCommissionPrecision { get; set; }
        public string quoteAsset { get; set; }
        public int quoteAssetPrecision { get; set; }
        public int quoteCommissionPrecision { get; set; }
        public string status { get; set; }


    }

    public class Ticker24hr
    {
        public string askPrice { get; set; }  
        public string askQty { get; set; }
        public string bidPrice { get; set; }
        public string bidQty { get; set; }
        public ulong closeTime { get; set; }
        public long count { get; set; }
        public long firstId { get; set; }
        public long lastId { get; set; }
        public string highPrice { get; set; }
        public string lastPrice { get; set; }
        public string lastQty { get; set; }
        public string lowPrice { get; set; }
        public string openPrice { get; set; }
        public ulong openTime { get; set; }
        public string prevClosePrice { get; set; }
        public string priceChange { get; set; }
        public string priceChangePercent { get; set; }
        public string quoteVolume { get; set; }
        public string symbol { get; set; }
        public string volume { get; set; }
        public string weightedAvgPrice { get; set; }

    }


}
