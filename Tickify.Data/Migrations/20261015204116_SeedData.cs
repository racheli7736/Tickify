using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tickify.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Music" },
                    { 2, "Theater" },
                    { 3, "Comedy" },
                    { 4, "Sports" },
                    { 5, "Festival" }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "StartsAt", "TicketPrice", "Venue" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "An open-air rock concert.", "Summer Rock Night", new DateTime(2028, 6, 15, 18, 0, 0, 0, DateTimeKind.Utc), 120m, "City Arena" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Classic theater production.", "Hamlet", new DateTime(2028, 7, 1, 20, 0, 0, 0, DateTimeKind.Utc), 90m, "National Theater" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Three comedians, one stage.", "Stand-Up Evening", new DateTime(2028, 8, 10, 21, 0, 0, 0, DateTimeKind.Utc), 60m, "Comedy Club" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@tickets.local", "Admin", "$2a$11$/c.bEkn0q5DhZ95twfl9ZuP5JUEMoVSovmr5xw2k6XYDI2HtHT7cC", "Admin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user@tickets.local", "Demo User", "$2a$11$gQWX5BeMMEcK2a9LxUloWe3xNioxKRB6NZEXPYRuhQoy/daYNRCOa", "User" }
                });

            migrationBuilder.InsertData(
                table: "EventCategories",
                columns: new[] { "CategoryId", "EventId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 5, 1 },
                    { 2, 2 },
                    { 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "EventId", "Number", "OrderId", "Row", "Status" },
                values: new object[,]
                {
                    { 1, 1, 1, null, 1, "Available" },
                    { 2, 1, 2, null, 1, "Available" },
                    { 3, 1, 3, null, 1, "Available" },
                    { 4, 1, 4, null, 1, "Available" },
                    { 5, 1, 5, null, 1, "Available" },
                    { 6, 1, 6, null, 1, "Available" },
                    { 7, 1, 7, null, 1, "Available" },
                    { 8, 1, 8, null, 1, "Available" },
                    { 9, 1, 9, null, 1, "Available" },
                    { 10, 1, 10, null, 1, "Available" },
                    { 11, 1, 1, null, 2, "Available" },
                    { 12, 1, 2, null, 2, "Available" },
                    { 13, 1, 3, null, 2, "Available" },
                    { 14, 1, 4, null, 2, "Available" },
                    { 15, 1, 5, null, 2, "Available" },
                    { 16, 1, 6, null, 2, "Available" },
                    { 17, 1, 7, null, 2, "Available" },
                    { 18, 1, 8, null, 2, "Available" },
                    { 19, 1, 9, null, 2, "Available" },
                    { 20, 1, 10, null, 2, "Available" },
                    { 21, 1, 1, null, 3, "Available" },
                    { 22, 1, 2, null, 3, "Available" },
                    { 23, 1, 3, null, 3, "Available" },
                    { 24, 1, 4, null, 3, "Available" },
                    { 25, 1, 5, null, 3, "Available" },
                    { 26, 1, 6, null, 3, "Available" },
                    { 27, 1, 7, null, 3, "Available" },
                    { 28, 1, 8, null, 3, "Available" },
                    { 29, 1, 9, null, 3, "Available" },
                    { 30, 1, 10, null, 3, "Available" },
                    { 31, 1, 1, null, 4, "Available" },
                    { 32, 1, 2, null, 4, "Available" },
                    { 33, 1, 3, null, 4, "Available" },
                    { 34, 1, 4, null, 4, "Available" },
                    { 35, 1, 5, null, 4, "Available" },
                    { 36, 1, 6, null, 4, "Available" },
                    { 37, 1, 7, null, 4, "Available" },
                    { 38, 1, 8, null, 4, "Available" },
                    { 39, 1, 9, null, 4, "Available" },
                    { 40, 1, 10, null, 4, "Available" },
                    { 41, 1, 1, null, 5, "Available" },
                    { 42, 1, 2, null, 5, "Available" },
                    { 43, 1, 3, null, 5, "Available" },
                    { 44, 1, 4, null, 5, "Available" },
                    { 45, 1, 5, null, 5, "Available" },
                    { 46, 1, 6, null, 5, "Available" },
                    { 47, 1, 7, null, 5, "Available" },
                    { 48, 1, 8, null, 5, "Available" },
                    { 49, 1, 9, null, 5, "Available" },
                    { 50, 1, 10, null, 5, "Available" },
                    { 51, 2, 1, null, 1, "Available" },
                    { 52, 2, 2, null, 1, "Available" },
                    { 53, 2, 3, null, 1, "Available" },
                    { 54, 2, 4, null, 1, "Available" },
                    { 55, 2, 5, null, 1, "Available" },
                    { 56, 2, 6, null, 1, "Available" },
                    { 57, 2, 7, null, 1, "Available" },
                    { 58, 2, 8, null, 1, "Available" },
                    { 59, 2, 1, null, 2, "Available" },
                    { 60, 2, 2, null, 2, "Available" },
                    { 61, 2, 3, null, 2, "Available" },
                    { 62, 2, 4, null, 2, "Available" },
                    { 63, 2, 5, null, 2, "Available" },
                    { 64, 2, 6, null, 2, "Available" },
                    { 65, 2, 7, null, 2, "Available" },
                    { 66, 2, 8, null, 2, "Available" },
                    { 67, 2, 1, null, 3, "Available" },
                    { 68, 2, 2, null, 3, "Available" },
                    { 69, 2, 3, null, 3, "Available" },
                    { 70, 2, 4, null, 3, "Available" },
                    { 71, 2, 5, null, 3, "Available" },
                    { 72, 2, 6, null, 3, "Available" },
                    { 73, 2, 7, null, 3, "Available" },
                    { 74, 2, 8, null, 3, "Available" },
                    { 75, 2, 1, null, 4, "Available" },
                    { 76, 2, 2, null, 4, "Available" },
                    { 77, 2, 3, null, 4, "Available" },
                    { 78, 2, 4, null, 4, "Available" },
                    { 79, 2, 5, null, 4, "Available" },
                    { 80, 2, 6, null, 4, "Available" },
                    { 81, 2, 7, null, 4, "Available" },
                    { 82, 2, 8, null, 4, "Available" },
                    { 83, 3, 1, null, 1, "Available" },
                    { 84, 3, 2, null, 1, "Available" },
                    { 85, 3, 3, null, 1, "Available" },
                    { 86, 3, 4, null, 1, "Available" },
                    { 87, 3, 5, null, 1, "Available" },
                    { 88, 3, 6, null, 1, "Available" },
                    { 89, 3, 7, null, 1, "Available" },
                    { 90, 3, 8, null, 1, "Available" },
                    { 91, 3, 9, null, 1, "Available" },
                    { 92, 3, 10, null, 1, "Available" },
                    { 93, 3, 1, null, 2, "Available" },
                    { 94, 3, 2, null, 2, "Available" },
                    { 95, 3, 3, null, 2, "Available" },
                    { 96, 3, 4, null, 2, "Available" },
                    { 97, 3, 5, null, 2, "Available" },
                    { 98, 3, 6, null, 2, "Available" },
                    { 99, 3, 7, null, 2, "Available" },
                    { 100, 3, 8, null, 2, "Available" },
                    { 101, 3, 9, null, 2, "Available" },
                    { 102, 3, 10, null, 2, "Available" },
                    { 103, 3, 1, null, 3, "Available" },
                    { 104, 3, 2, null, 3, "Available" },
                    { 105, 3, 3, null, 3, "Available" },
                    { 106, 3, 4, null, 3, "Available" },
                    { 107, 3, 5, null, 3, "Available" },
                    { 108, 3, 6, null, 3, "Available" },
                    { 109, 3, 7, null, 3, "Available" },
                    { 110, 3, 8, null, 3, "Available" },
                    { 111, 3, 9, null, 3, "Available" },
                    { 112, 3, 10, null, 3, "Available" },
                    { 113, 3, 1, null, 4, "Available" },
                    { 114, 3, 2, null, 4, "Available" },
                    { 115, 3, 3, null, 4, "Available" },
                    { 116, 3, 4, null, 4, "Available" },
                    { 117, 3, 5, null, 4, "Available" },
                    { 118, 3, 6, null, 4, "Available" },
                    { 119, 3, 7, null, 4, "Available" },
                    { 120, 3, 8, null, 4, "Available" },
                    { 121, 3, 9, null, 4, "Available" },
                    { 122, 3, 10, null, 4, "Available" },
                    { 123, 3, 1, null, 5, "Available" },
                    { 124, 3, 2, null, 5, "Available" },
                    { 125, 3, 3, null, 5, "Available" },
                    { 126, 3, 4, null, 5, "Available" },
                    { 127, 3, 5, null, 5, "Available" },
                    { 128, 3, 6, null, 5, "Available" },
                    { 129, 3, 7, null, 5, "Available" },
                    { 130, 3, 8, null, 5, "Available" },
                    { 131, 3, 9, null, 5, "Available" },
                    { 132, 3, 10, null, 5, "Available" },
                    { 133, 3, 1, null, 6, "Available" },
                    { 134, 3, 2, null, 6, "Available" },
                    { 135, 3, 3, null, 6, "Available" },
                    { 136, 3, 4, null, 6, "Available" },
                    { 137, 3, 5, null, 6, "Available" },
                    { 138, 3, 6, null, 6, "Available" },
                    { 139, 3, 7, null, 6, "Available" },
                    { 140, 3, 8, null, 6, "Available" },
                    { 141, 3, 9, null, 6, "Available" },
                    { 142, 3, 10, null, 6, "Available" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EventCategories",
                keyColumns: new[] { "CategoryId", "EventId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "EventCategories",
                keyColumns: new[] { "CategoryId", "EventId" },
                keyValues: new object[] { 5, 1 });

            migrationBuilder.DeleteData(
                table: "EventCategories",
                keyColumns: new[] { "CategoryId", "EventId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "EventCategories",
                keyColumns: new[] { "CategoryId", "EventId" },
                keyValues: new object[] { 3, 3 });

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "Seats",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
