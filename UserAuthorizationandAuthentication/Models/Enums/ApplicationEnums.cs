using System.Text.Json.Serialization;

namespace TravAi.Models.Enums
{


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentType
    {
        TaxCard,
        CommercialRegistry,
        OwnershipLeaseProof,
        AddressProof,
        RealPhotos,
        CivilProtectionCertificate,
        HotelClassificationCertificate,
        HealthCertificate,
        BankAccountDetails,
        OperatingLicense
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VerificationStatus
    {
        Pending,
        Uploaded,
        Verified,
        Approved,
        Rejected,
        Suspended,
        Banned
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CancellationStrategy
    {
        FreeAll,
        NonRefundable,
        WindowBased
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HotelContactType
    {
        Email,
        Phone,
        Fax
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HotelFieldType
    {
        Text,
        TextArea,
        Number,
        Date,
        File
    }
}
