using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaWagyu.Migrations
{
    /// <inheritdoc />
    public partial class FixRelacionamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdemServico_Pecas_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Pecas");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdemServico_Servicos_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Servicos");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdemServico_Pecas_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Pecas",
                column: "OrdemServicoId",
                principalTable: "OrdensServico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdemServico_Servicos_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Servicos",
                column: "OrdemServicoId",
                principalTable: "OrdensServico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdemServico_Pecas_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Pecas");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdemServico_Servicos_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Servicos");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdemServico_Pecas_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Pecas",
                column: "OrdemServicoId",
                principalTable: "OrdensServico",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdemServico_Servicos_OrdensServico_OrdemServicoId",
                table: "OrdemServico_Servicos",
                column: "OrdemServicoId",
                principalTable: "OrdensServico",
                principalColumn: "Id");
        }
    }
}
