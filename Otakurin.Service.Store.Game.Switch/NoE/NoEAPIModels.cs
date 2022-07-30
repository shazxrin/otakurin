using System.Text.Json.Serialization;

namespace Otakurin.Service.Store.Game.Switch.NoE;
public class NintendoSearchAPIDoc
{
    [JsonPropertyName("nsuid_txt")]
    public List<string>? NsuidTxt { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class NintendoSearchAPIResponse
{
    [JsonPropertyName("docs")]
    public List<NintendoSearchAPIDoc>? Docs { get; set; }
}

public class NintendoSearchAPIBody
{
    [JsonPropertyName("response")]
    public NintendoSearchAPIResponse? Response { get; set; }
}

public class NintendoPriceAPIDiscountPrice
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("raw_value")]
    public double Amount { get; set; }

    [JsonPropertyName("start_datetime")]
    public DateTime StartDatetime { get; set; }

    [JsonPropertyName("end_datetime")]
    public DateTime EndDatetime { get; set; }
}


public class NintendoPriceAPIRegularPrice
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("raw_value")]
    public double Amount { get; set; }
}

public class NintendoPriceAPIPrice
{
    [JsonPropertyName("regular_price")]
    public NintendoPriceAPIRegularPrice? RegularPrice { get; set; }

    [JsonPropertyName("discount_price")]
    public NintendoPriceAPIDiscountPrice? DiscountPrice { get; set; }
}

public class NintendoPriceAPIResponse
{
    [JsonPropertyName("prices")]
    public List<NintendoPriceAPIPrice>? Prices { get; set; }
}

