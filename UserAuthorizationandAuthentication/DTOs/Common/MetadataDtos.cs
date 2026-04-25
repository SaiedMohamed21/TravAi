namespace UserAuthorizationandAuthentication.DTOs.Common
{
    public class AmenityDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsHighlighted { get; set; }
    }

    public class DocumentTypeDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
    }

    public class HotelFieldDefinitionDto
    {
        public long Id { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FieldType { get; set; } = "Text"; // Text, Number, Select
        public bool IsRequired { get; set; }
    }
}
