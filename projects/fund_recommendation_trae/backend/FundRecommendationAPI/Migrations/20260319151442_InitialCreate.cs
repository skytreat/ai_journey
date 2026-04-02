using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundRecommendationAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fund_asset_scale",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    asset_scale = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    share_scale = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_asset_scale", x => new { x.code, x.date });
                });

            migrationBuilder.CreateTable(
                name: "fund_basic_info",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    fund_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    share_type = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    main_fund_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    establish_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    list_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    manager = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    custodian = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    management_fee_rate = table.Column<decimal>(type: "DECIMAL(5,4)", nullable: false),
                    custodian_fee_rate = table.Column<decimal>(type: "DECIMAL(5,4)", nullable: false),
                    sales_fee_rate = table.Column<decimal>(type: "DECIMAL(5,4)", nullable: true),
                    benchmark = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    tracking_target = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    investment_style = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    risk_level = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_basic_info", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "fund_corporate_actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ex_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    dividend_per_share = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    split_ratio = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: true),
                    record_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    event_description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    announcement_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_corporate_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fund_manager",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    manager_name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    end_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    tenure = table.Column<decimal>(type: "DECIMAL(5,1)", nullable: false),
                    manage_days = table.Column<int>(type: "INTEGER", nullable: false),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_manager", x => new { x.code, x.manager_name, x.start_date });
                });

            migrationBuilder.CreateTable(
                name: "fund_purchase_status",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    purchase_status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    purchase_limit = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    purchase_fee_rate = table.Column<decimal>(type: "DECIMAL(5,4)", nullable: true),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_purchase_status", x => new { x.code, x.date });
                });

            migrationBuilder.CreateTable(
                name: "fund_redemption_status",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    redemption_status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    redemption_limit = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    redemption_fee_rate = table.Column<decimal>(type: "DECIMAL(5,4)", nullable: true),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_redemption_status", x => new { x.code, x.date });
                });

            migrationBuilder.CreateTable(
                name: "SystemUpdateHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordsUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUpdateHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite_funds",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    fund_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    add_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    group_tag = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    alert_settings = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_funds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    fund_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    score_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    total_score = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    return_score = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    risk_score = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    risk_adjusted_return_score = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    rank_score = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    score_change = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    score_trend = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    weight_config = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    calculate_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_scores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fund_nav_history",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    nav = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: false),
                    accumulated_nav = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: false),
                    daily_growth_rate = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_nav_history", x => new { x.code, x.date });
                    table.ForeignKey(
                        name: "FK_fund_nav_history_fund_basic_info_code",
                        column: x => x.code,
                        principalTable: "fund_basic_info",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fund_performance",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    period_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    period_value = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    nav_growth_rate = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: false),
                    max_drawdown = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    downside_std = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    sharpe_ratio = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    sortino_ratio = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    calmar_ratio = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    annual_return = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    volatility = table.Column<decimal>(type: "DECIMAL(8,4)", nullable: true),
                    rank_in_category = table.Column<int>(type: "INTEGER", nullable: true),
                    total_in_category = table.Column<int>(type: "INTEGER", nullable: true),
                    update_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_performance", x => x.id);
                    table.ForeignKey(
                        name: "FK_fund_performance_fund_basic_info_code",
                        column: x => x.code,
                        principalTable: "fund_basic_info",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fund_asset_scale_code_date",
                table: "fund_asset_scale",
                columns: new[] { "code", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fund_basic_info_code",
                table: "fund_basic_info",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fund_basic_info_fund_type",
                table: "fund_basic_info",
                column: "fund_type");

            migrationBuilder.CreateIndex(
                name: "IX_fund_basic_info_manager",
                table: "fund_basic_info",
                column: "manager");

            migrationBuilder.CreateIndex(
                name: "IX_fund_basic_info_risk_level",
                table: "fund_basic_info",
                column: "risk_level");

            migrationBuilder.CreateIndex(
                name: "IX_fund_corporate_actions_code_ex_date",
                table: "fund_corporate_actions",
                columns: new[] { "code", "ex_date" });

            migrationBuilder.CreateIndex(
                name: "IX_fund_corporate_actions_event_type",
                table: "fund_corporate_actions",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_fund_manager_code",
                table: "fund_manager",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_fund_nav_history_code_date",
                table: "fund_nav_history",
                columns: new[] { "code", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fund_nav_history_date",
                table: "fund_nav_history",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_fund_performance_code_period_type",
                table: "fund_performance",
                columns: new[] { "code", "period_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fund_performance_period_type_code_nav_growth_rate",
                table: "fund_performance",
                columns: new[] { "period_type", "code", "nav_growth_rate" });

            migrationBuilder.CreateIndex(
                name: "IX_fund_purchase_status_code_date",
                table: "fund_purchase_status",
                columns: new[] { "code", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fund_redemption_status_code_date",
                table: "fund_redemption_status",
                columns: new[] { "code", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdateHistory_CreatedAt",
                table: "SystemUpdateHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdateHistory_Status",
                table: "SystemUpdateHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_funds_user_id",
                table: "user_favorite_funds",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_funds_user_id_fund_code",
                table: "user_favorite_funds",
                columns: new[] { "user_id", "fund_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_scores_user_id",
                table: "user_favorite_scores",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_scores_user_id_fund_code_score_date",
                table: "user_favorite_scores",
                columns: new[] { "user_id", "fund_code", "score_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fund_asset_scale");

            migrationBuilder.DropTable(
                name: "fund_corporate_actions");

            migrationBuilder.DropTable(
                name: "fund_manager");

            migrationBuilder.DropTable(
                name: "fund_nav_history");

            migrationBuilder.DropTable(
                name: "fund_performance");

            migrationBuilder.DropTable(
                name: "fund_purchase_status");

            migrationBuilder.DropTable(
                name: "fund_redemption_status");

            migrationBuilder.DropTable(
                name: "SystemUpdateHistory");

            migrationBuilder.DropTable(
                name: "user_favorite_funds");

            migrationBuilder.DropTable(
                name: "user_favorite_scores");

            migrationBuilder.DropTable(
                name: "fund_basic_info");
        }
    }
}
