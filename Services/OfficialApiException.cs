namespace DCTravelCli.Services;

public sealed class OfficialApiException(int returnCode, string message) : Exception(message)
{
    public int ReturnCode { get; } = returnCode;
}
