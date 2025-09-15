using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketingSystem.Migrations
{
    public partial class AddConcessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the tables first
            migrationBuilder.CreateTable(
                name: "Concessions",
                columns: table => new
                {
                    ConcessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsVegetarian = table.Column<bool>(type: "bit", nullable: false),
                    IsVegan = table.Column<bool>(type: "bit", nullable: false),
                    ContainsNuts = table.Column<bool>(type: "bit", nullable: false),
                    ContainsDairy = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concessions", x => x.ConcessionId);
                });

            migrationBuilder.CreateTable(
                name: "ConcessionOrders",
                columns: table => new
                {
                    ConcessionOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcessionOrders", x => x.ConcessionOrderId);
                    table.ForeignKey(
                        name: "FK_ConcessionOrders_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcessionOrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConcessionOrderId = table.Column<int>(type: "int", nullable: false),
                    ConcessionId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcessionOrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_ConcessionOrderItems_ConcessionOrders_ConcessionOrderId",
                        column: x => x.ConcessionOrderId,
                        principalTable: "ConcessionOrders",
                        principalColumn: "ConcessionOrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConcessionOrderItems_Concessions_ConcessionId",
                        column: x => x.ConcessionId,
                        principalTable: "Concessions",
                        principalColumn: "ConcessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            // ADD SAMPLE CONCESSIONS DATA
            migrationBuilder.InsertData(
                table: "Concessions",
                columns: new[] {
                    "Name",
                    "Description",
                    "Price",
                    "Category",
                    "ImageUrl",
                    "IsAvailable",
                    "StockQuantity",
                    "IsVegetarian",
                    "IsVegan",
                    "ContainsNuts",
                    "ContainsDairy"
                },
                values: new object[,]
                {
                    // Popcorn Category
                    {
                        "Small Popcorn",
                        "Buttery popcorn, regular size",
                        6.99m,
                        "Popcorn",
                        "/images/concessions/popcorn-small.jpg",
                        true,
                        100,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Medium Popcorn",
                        "Buttery popcorn, medium size",
                        8.99m,
                        "Popcorn",
                        "/images/concessions/popcorn-medium.jpg",
                        true,
                        100,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Large Popcorn",
                        "Buttery popcorn, large size",
                        10.99m,
                        "Popcorn",
                        "/images/concessions/popcorn-large.jpg",
                        true,
                        100,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Caramel Popcorn",
                        "Sweet caramel coated popcorn",
                        9.99m,
                        "Popcorn",
                        "/images/concessions/popcorn-caramel.jpg",
                        true,
                        80,
                        true,
                        false,
                        false,
                        true
                    },

                    // Drinks Category
                    {
                        "Small Drink",
                        "Coca-Cola, Pepsi, or Sprite - 16oz",
                        4.99m,
                        "Drinks",
                        "/images/concessions/drink-small.jpg",
                        true,
                        200,
                        true,
                        true,
                        false,
                        false
                    },
                    {
                        "Medium Drink",
                        "Coca-Cola, Pepsi, or Sprite - 22oz",
                        5.99m,
                        "Drinks",
                        "/images/concessions/drink-medium.jpg",
                        true,
                        200,
                        true,
                        true,
                        false,
                        false
                    },
                    {
                        "Large Drink",
                        "Coca-Cola, Pepsi, or Sprite - 32oz",
                        6.99m,
                        "Drinks",
                        "/images/concessions/drink-large.jpg",
                        true,
                        200,
                        true,
                        true,
                        false,
                        false
                    },
                    {
                        "Bottled Water",
                        "500ml purified water",
                        3.50m,
                        "Drinks",
                        "/images/concessions/water.jpg",
                        true,
                        150,
                        true,
                        true,
                        false,
                        false
                    },

                    // Candy Category
                    {
                        "Chocolate Bar",
                        "Milky Way or Snickers",
                        3.99m,
                        "Candy",
                        "/images/concessions/chocolate.jpg",
                        true,
                        150,
                        true,
                        false,
                        true,
                        true
                    },
                    {
                        "Gummy Bears",
                        "Assorted fruit flavors",
                        3.50m,
                        "Candy",
                        "/images/concessions/gummies.jpg",
                        true,
                        150,
                        true,
                        true,
                        false,
                        false
                    },
                    {
                        "M&M's",
                        "Milk chocolate candies",
                        4.25m,
                        "Candy",
                        "/images/concessions/mms.jpg",
                        true,
                        120,
                        true,
                        false,
                        true,
                        true
                    },
                    {
                        "Skittles",
                        "Taste the rainbow",
                        3.75m,
                        "Candy",
                        "/images/concessions/skittles.jpg",
                        true,
                        120,
                        true,
                        true,
                        false,
                        false
                    },

                    // Snacks Category
                    {
                        "Nachos",
                        "Tortilla chips with cheese sauce",
                        7.99m,
                        "Snacks",
                        "/images/concessions/nachos.jpg",
                        true,
                        80,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Hot Dog",
                        "Beef hot dog with bun",
                        5.99m,
                        "Snacks",
                        "/images/concessions/hotdog.jpg",
                        true,
                        60,
                        false,
                        false,
                        false,
                        false
                    },
                    {
                        "Pretzel",
                        "Soft pretzel with salt",
                        4.50m,
                        "Snacks",
                        "/images/concessions/pretzel.jpg",
                        true,
                        70,
                        true,
                        false,
                        false,
                        true
                    },

                    // Combo Deals
                    {
                        "Combo #1",
                        "Medium popcorn + medium drink",
                        12.99m,
                        "Combo",
                        "/images/concessions/combo1.jpg",
                        true,
                        100,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Combo #2",
                        "Large popcorn + large drink + chocolate",
                        18.99m,
                        "Combo",
                        "/images/concessions/combo2.jpg",
                        true,
                        100,
                        true,
                        false,
                        true,
                        true
                    },
                    {
                        "Combo #3",
                        "2 Small popcorns + 2 Small drinks",
                        20.99m,
                        "Combo",
                        "/images/concessions/combo3.jpg",
                        true,
                        80,
                        true,
                        false,
                        false,
                        true
                    },
                    {
                        "Family Pack",
                        "2 Large popcorns + 4 Medium drinks + 2 candies",
                        35.99m,
                        "Combo",
                        "/images/concessions/family-pack.jpg",
                        true,
                        50,
                        true,
                        false,
                        true,
                        true
                    }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConcessionOrderItems_ConcessionId",
                table: "ConcessionOrderItems",
                column: "ConcessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConcessionOrderItems_ConcessionOrderId",
                table: "ConcessionOrderItems",
                column: "ConcessionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConcessionOrders_BookingId",
                table: "ConcessionOrders",
                column: "BookingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConcessionOrderItems");

            migrationBuilder.DropTable(
                name: "ConcessionOrders");

            migrationBuilder.DropTable(
                name: "Concessions");
        }
    }
}