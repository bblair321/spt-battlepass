using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using SptBattlePass.Client.Models;

namespace SptBattlePass.Client.Services;

public static class BattlePassApi
{
    public const string StatusRoute = "/client/battlepass/status";
    public const string RaidEndRoute = "/client/battlepass/raidend";
    public const string BuyRoute = "/client/battlepass/buy";
    public const string GrantRoute = "/client/battlepass/grant";
    public const string RerollRoute = "/client/battlepass/reroll";
    public const string HandoverRoute = "/client/battlepass/handover";
    public const string PremiumRoute = "/client/battlepass/premium";

    public static async Task<BattlePassStatusDto> FetchStatusAsync()
    {
        byte[] bytes = await RequestHandler.GetDataAsync(StatusRoute);
        string json = Decode(bytes);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass status response.");
        }

        JObject envelope = JObject.Parse(json);
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass status had no data field.");
        }

        return data.ToObject<BattlePassStatusDto>()
               ?? JsonConvert.DeserializeObject<BattlePassStatusDto>(data.ToString());
    }

    public static async Task<RaidEndResultDto> ReportRaidAsync(RaidResultDto result)
    {
        string payload = JsonConvert.SerializeObject(result);
        string json = await RequestHandler.PostJsonAsync(RaidEndRoute, payload);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass raidend response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass raidend had no data field.");
        }

        return data.ToObject<RaidEndResultDto>()
               ?? JsonConvert.DeserializeObject<RaidEndResultDto>(data.ToString());
    }

    public static async Task<BuyResultDto> BuyAsync(string offerId)
    {
        string payload = JsonConvert.SerializeObject(new { id = offerId });
        string json = await RequestHandler.PostJsonAsync(BuyRoute, payload);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass buy response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass buy had no data field.");
        }

        return data.ToObject<BuyResultDto>()
               ?? JsonConvert.DeserializeObject<BuyResultDto>(data.ToString());
    }

    public static async Task<GrantResultDto> GrantAsync()
    {
        string json = await RequestHandler.PostJsonAsync(GrantRoute, "{}");
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass grant response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass grant had no data field.");
        }

        return data.ToObject<GrantResultDto>()
               ?? JsonConvert.DeserializeObject<GrantResultDto>(data.ToString());
    }

    public static async Task<RerollResultDto> RerollAsync(string bucket)
    {
        string payload = JsonConvert.SerializeObject(new { bucket });
        string json = await RequestHandler.PostJsonAsync(RerollRoute, payload);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass reroll response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass reroll had no data field.");
        }

        return data.ToObject<RerollResultDto>()
               ?? JsonConvert.DeserializeObject<RerollResultDto>(data.ToString());
    }

    public static async Task<HandoverResultDto> HandoverAsync(string instanceId)
    {
        string payload = JsonConvert.SerializeObject(new { instanceId });
        string json = await RequestHandler.PostJsonAsync(HandoverRoute, payload);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass handover response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass handover had no data field.");
        }

        return data.ToObject<HandoverResultDto>()
               ?? JsonConvert.DeserializeObject<HandoverResultDto>(data.ToString());
    }

    public static async Task<PremiumResultDto> UnlockPremiumAsync(bool debug)
    {
        string payload = JsonConvert.SerializeObject(new { debug });
        string json = await RequestHandler.PostJsonAsync(PremiumRoute, payload);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty battle pass premium response.");
        }

        JObject envelope = JObject.Parse(DecodeString(json));
        JToken data = envelope["data"];
        if (data == null || data.Type == JTokenType.Null)
        {
            throw new InvalidOperationException("Battle pass premium had no data field.");
        }

        return data.ToObject<PremiumResultDto>()
               ?? JsonConvert.DeserializeObject<PremiumResultDto>(data.ToString());
    }

    private static string DecodeString(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return json;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string decoded = Decode(bytes);
        return string.IsNullOrEmpty(decoded) ? json : decoded;
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return string.Empty;
        }

        if (bytes.Length >= 2 && bytes[0] == 0x78)
        {
            try
            {
                using var input = new MemoryStream(bytes, 2, bytes.Length - 2);
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(deflate, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                // Fall through to raw UTF-8 if the payload was not zlib.
            }
        }

        return Encoding.UTF8.GetString(bytes);
    }
}
