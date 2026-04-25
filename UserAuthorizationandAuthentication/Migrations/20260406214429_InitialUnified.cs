using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAuthorizationandAuthentication.Migrations
{
    /// <inheritdoc />
    public partial class InitialUnified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airline_Airports",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Airports", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "hotel_Amenities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_Amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hotel_DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    ProfilePic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    IsBanned = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airline_Airlines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Airlines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_Airlines_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_UserCompanions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProfilePic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_UserCompanions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_UserCompanions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_WalletTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_Hotels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Governorate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StarRating = table.Column<int>(type: "int", nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccommodationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressProofUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TypeNorm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumReviews = table.Column<int>(type: "int", nullable: false),
                    AvgReviewScore = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_Hotels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_Hotels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourGuides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseCard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseIdFrontPhoto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseIdBackPhoto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Certification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    SuspendedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourGuides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPhones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: false),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    PhoneVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPhones_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_Flights",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartureAirportCode = table.Column<string>(type: "nvarchar(3)", nullable: false),
                    ArrivalAirportCode = table.Column<string>(type: "nvarchar(3)", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableSeats = table.Column<int>(type: "int", nullable: false),
                    AirlineId = table.Column<long>(type: "bigint", nullable: false),
                    NumberOfStops = table.Column<int>(type: "int", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinationImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_Flights_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_airline_Flights_airline_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "airline_Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_airline_Flights_airline_Airports_ArrivalAirportCode",
                        column: x => x.ArrivalAirportCode,
                        principalTable: "airline_Airports",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_airline_Flights_airline_Airports_DepartureAirportCode",
                        column: x => x.DepartureAirportCode,
                        principalTable: "airline_Airports",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelAmenities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    AmenityId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelAmenities_hotel_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "hotel_Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_HotelAmenities_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelBookings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Nights = table.Column<int>(type: "int", nullable: true),
                    TotalRooms = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelBookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_HotelBookings_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelContacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    ContactType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelContacts_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelDocuments_hotel_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "hotel_DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_HotelDocuments_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelFieldValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    FieldDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelFieldValues_hotel_HotelFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "hotel_HotelFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_HotelFieldValues_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelImages_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceChargePct = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludeServiceCharge = table.Column<bool>(type: "bit", nullable: false),
                    IncludeVat = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCityTax = table.Column<bool>(type: "bit", nullable: false),
                    CancellationStrategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelPolicies_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_HotelReviews_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelRooms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Occupancy = table.Column<int>(type: "int", nullable: true),
                    BedType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ROPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BBPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HBPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FBPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AIPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelRooms_hotel_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotel_Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourGuideCities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourGuideCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourGuideCities_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourGuideEmails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourGuideEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourGuideEmails_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourGuideLanguages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    Language = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourGuideLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourGuideLanguages_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourGuidePhones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourGuidePhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourGuidePhones_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_Tours",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TourTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TourType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TourDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePriceUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: true),
                    GroupSizeMax = table.Column<int>(type: "int", nullable: true),
                    SitesCovered = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    NumberOfReviews = table.Column<int>(type: "int", nullable: true),
                    StartingPoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgeRestriction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransportIncluded = table.Column<bool>(type: "bit", nullable: false),
                    MealsIncluded = table.Column<bool>(type: "bit", nullable: false),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false),
                    Accessibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Customizable = table.Column<bool>(type: "bit", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludedServices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExcludedServices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SafetyMeasures = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BestTimeToVisit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PickupDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourcePlatform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailableDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationPolicy = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_Tours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_Tours_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_WithdrawRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_WithdrawRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_WithdrawRequests_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_Bookings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    NumberOfSeats = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_Bookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_airline_Bookings_airline_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "airline_Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "airline_Reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_airline_Reviews_airline_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "airline_Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelCancellationRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelPolicyId = table.Column<long>(type: "bigint", nullable: false),
                    FromHoursBeforeCheckIn = table.Column<int>(type: "int", nullable: true),
                    ToHoursBeforeCheckIn = table.Column<int>(type: "int", nullable: true),
                    PenaltyPct = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelCancellationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelCancellationRules_hotel_HotelPolicies_HotelPolicyId",
                        column: x => x.HotelPolicyId,
                        principalTable: "hotel_HotelPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_HotelBookingRooms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    RoomName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MealPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Nights = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_HotelBookingRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_HotelBookingRooms_hotel_HotelBookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "hotel_HotelBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hotel_HotelBookingRooms_hotel_HotelRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "hotel_HotelRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_Reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: true),
                    TourId = table.Column<long>(type: "bigint", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tourguide_Reviews_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tourguide_Reviews_tourguide_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "tourguide_Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourBookings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TourId = table.Column<long>(type: "bigint", nullable: false),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TourDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TourTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ParticipantsCount = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookings_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookings_tourguide_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "tourguide_Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourId = table.Column<long>(type: "bigint", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourImages_tourguide_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "tourguide_Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_UrgentRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourGuideId = table.Column<long>(type: "bigint", nullable: false),
                    TourId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentationUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_UrgentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_UrgentRequests_tourguide_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalTable: "tourguide_TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tourguide_UrgentRequests_tourguide_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "tourguide_Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFromAdmin = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_ChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_airline_ChatMessages_airline_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "airline_Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_Passengers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    PassengerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_Passengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_Passengers_airline_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "airline_Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourBookingParticipants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    AgeType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialNeeds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DietaryRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourBookingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingParticipants_tourguide_TourBookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "tourguide_TourBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourBookingPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserSavedCardId = table.Column<long>(type: "bigint", nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    PlatformCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProviderNetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayoutStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourBookingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tourguide_TourBookingPayments_tourguide_TourBookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "tourguide_TourBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_PassengerEmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PassengerId = table.Column<long>(type: "bigint", nullable: false),
                    EmergencyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_PassengerEmergencyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_PassengerEmergencyContacts_airline_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "airline_Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "airline_PassengerPhones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PassengerId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_PassengerPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_airline_PassengerPhones_airline_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "airline_Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourParticipantEmergencyNumbers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<long>(type: "bigint", nullable: false),
                    EmergencyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourParticipantEmergencyNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourParticipantEmergencyNumbers_tourguide_TourBookingParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "tourguide_TourBookingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tourguide_TourParticipantPhones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tourguide_TourParticipantPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tourguide_TourParticipantPhones_tourguide_TourBookingParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "tourguide_TourBookingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airline_Airlines_UserId",
                table: "airline_Airlines",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Bookings_FlightId",
                table: "airline_Bookings",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Bookings_UserId",
                table: "airline_Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_ChatMessages_BookingId",
                table: "airline_ChatMessages",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_ChatMessages_SenderId",
                table: "airline_ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Flights_AirlineId",
                table: "airline_Flights",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Flights_ArrivalAirportCode",
                table: "airline_Flights",
                column: "ArrivalAirportCode");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Flights_CreatedByUserId",
                table: "airline_Flights",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Flights_DepartureAirportCode",
                table: "airline_Flights",
                column: "DepartureAirportCode");

            migrationBuilder.CreateIndex(
                name: "IX_airline_PassengerEmergencyContacts_PassengerId",
                table: "airline_PassengerEmergencyContacts",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_PassengerPhones_PassengerId",
                table: "airline_PassengerPhones",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Passengers_BookingId",
                table: "airline_Passengers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Reviews_FlightId",
                table: "airline_Reviews",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_Reviews_UserId",
                table: "airline_Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_UserCompanions_UserId",
                table: "airline_UserCompanions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_airline_WalletTransactions_UserId",
                table: "airline_WalletTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_DocumentTypes_KeyName",
                table: "hotel_DocumentTypes",
                column: "KeyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelAmenities_AmenityId",
                table: "hotel_HotelAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelAmenities_HotelId",
                table: "hotel_HotelAmenities",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelBookingRooms_BookingId",
                table: "hotel_HotelBookingRooms",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelBookingRooms_RoomId",
                table: "hotel_HotelBookingRooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelBookings_HotelId",
                table: "hotel_HotelBookings",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelBookings_UserId",
                table: "hotel_HotelBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelCancellationRules_HotelPolicyId",
                table: "hotel_HotelCancellationRules",
                column: "HotelPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelContacts_HotelId",
                table: "hotel_HotelContacts",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelDocuments_DocumentTypeId",
                table: "hotel_HotelDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelDocuments_HotelId",
                table: "hotel_HotelDocuments",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelFieldDefinitions_KeyName",
                table: "hotel_HotelFieldDefinitions",
                column: "KeyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelFieldValues_FieldDefinitionId",
                table: "hotel_HotelFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelFieldValues_HotelId",
                table: "hotel_HotelFieldValues",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelImages_HotelId",
                table: "hotel_HotelImages",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelPolicies_HotelId",
                table: "hotel_HotelPolicies",
                column: "HotelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelReviews_HotelId",
                table: "hotel_HotelReviews",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelReviews_UserId",
                table: "hotel_HotelReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_HotelRooms_HotelId",
                table: "hotel_HotelRooms",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_Hotels_UserId",
                table: "hotel_Hotels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Reviews_TourGuideId",
                table: "tourguide_Reviews",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Reviews_TourId",
                table: "tourguide_Reviews",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Reviews_UserId",
                table: "tourguide_Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingParticipants_BookingId",
                table: "tourguide_TourBookingParticipants",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingPayments_BookingId",
                table: "tourguide_TourBookingPayments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookingPayments_UserId",
                table: "tourguide_TourBookingPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookings_TourGuideId",
                table: "tourguide_TourBookings",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookings_TourId",
                table: "tourguide_TourBookings",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourBookings_UserId",
                table: "tourguide_TourBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourGuideCities_TourGuideId",
                table: "tourguide_TourGuideCities",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourGuideEmails_TourGuideId",
                table: "tourguide_TourGuideEmails",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourGuideLanguages_TourGuideId",
                table: "tourguide_TourGuideLanguages",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourGuidePhones_TourGuideId",
                table: "tourguide_TourGuidePhones",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourGuides_UserId",
                table: "tourguide_TourGuides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourImages_TourId",
                table: "tourguide_TourImages",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourParticipantEmergencyNumbers_ParticipantId",
                table: "tourguide_TourParticipantEmergencyNumbers",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_TourParticipantPhones_ParticipantId",
                table: "tourguide_TourParticipantPhones",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_Tours_TourGuideId",
                table: "tourguide_Tours",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_UrgentRequests_TourGuideId",
                table: "tourguide_UrgentRequests",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_UrgentRequests_TourId",
                table: "tourguide_UrgentRequests",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_tourguide_WithdrawRequests_TourGuideId",
                table: "tourguide_WithdrawRequests",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_UserId",
                table: "UserPhones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airline_ChatMessages");

            migrationBuilder.DropTable(
                name: "airline_PassengerEmergencyContacts");

            migrationBuilder.DropTable(
                name: "airline_PassengerPhones");

            migrationBuilder.DropTable(
                name: "airline_Reviews");

            migrationBuilder.DropTable(
                name: "airline_UserCompanions");

            migrationBuilder.DropTable(
                name: "airline_WalletTransactions");

            migrationBuilder.DropTable(
                name: "hotel_HotelAmenities");

            migrationBuilder.DropTable(
                name: "hotel_HotelBookingRooms");

            migrationBuilder.DropTable(
                name: "hotel_HotelCancellationRules");

            migrationBuilder.DropTable(
                name: "hotel_HotelContacts");

            migrationBuilder.DropTable(
                name: "hotel_HotelDocuments");

            migrationBuilder.DropTable(
                name: "hotel_HotelFieldValues");

            migrationBuilder.DropTable(
                name: "hotel_HotelImages");

            migrationBuilder.DropTable(
                name: "hotel_HotelReviews");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "tourguide_Reviews");

            migrationBuilder.DropTable(
                name: "tourguide_TourBookingPayments");

            migrationBuilder.DropTable(
                name: "tourguide_TourGuideCities");

            migrationBuilder.DropTable(
                name: "tourguide_TourGuideEmails");

            migrationBuilder.DropTable(
                name: "tourguide_TourGuideLanguages");

            migrationBuilder.DropTable(
                name: "tourguide_TourGuidePhones");

            migrationBuilder.DropTable(
                name: "tourguide_TourImages");

            migrationBuilder.DropTable(
                name: "tourguide_TourParticipantEmergencyNumbers");

            migrationBuilder.DropTable(
                name: "tourguide_TourParticipantPhones");

            migrationBuilder.DropTable(
                name: "tourguide_UrgentRequests");

            migrationBuilder.DropTable(
                name: "tourguide_WithdrawRequests");

            migrationBuilder.DropTable(
                name: "UserPhones");

            migrationBuilder.DropTable(
                name: "airline_Passengers");

            migrationBuilder.DropTable(
                name: "hotel_Amenities");

            migrationBuilder.DropTable(
                name: "hotel_HotelBookings");

            migrationBuilder.DropTable(
                name: "hotel_HotelRooms");

            migrationBuilder.DropTable(
                name: "hotel_HotelPolicies");

            migrationBuilder.DropTable(
                name: "hotel_DocumentTypes");

            migrationBuilder.DropTable(
                name: "hotel_HotelFieldDefinitions");

            migrationBuilder.DropTable(
                name: "tourguide_TourBookingParticipants");

            migrationBuilder.DropTable(
                name: "airline_Bookings");

            migrationBuilder.DropTable(
                name: "hotel_Hotels");

            migrationBuilder.DropTable(
                name: "tourguide_TourBookings");

            migrationBuilder.DropTable(
                name: "airline_Flights");

            migrationBuilder.DropTable(
                name: "tourguide_Tours");

            migrationBuilder.DropTable(
                name: "airline_Airlines");

            migrationBuilder.DropTable(
                name: "airline_Airports");

            migrationBuilder.DropTable(
                name: "tourguide_TourGuides");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
