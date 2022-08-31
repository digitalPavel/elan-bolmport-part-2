using Newtonsoft.Json;

namespace OrderProperty;

internal class Order
{
    [JsonProperty("ISBN")]
    public string isbn { get; set; }
    [JsonProperty("curPeriod")]
    public string period { get; set; }
    [JsonProperty("curMthGrsUnits")]
    public string soldUnits { get; set; }
    [JsonProperty("curMthGrsSales")]
    public string valueSold { get; set; }
    [JsonProperty("curMthRtnUnits")]
    public string returendUnits { get; set; }
    [JsonProperty("curMthRtnSales")]
    public string valueReturned { get; set; }
    [JsonProperty("curMthGrsUnitsCmp")]
    public string freeUnits { get; set; }
}
