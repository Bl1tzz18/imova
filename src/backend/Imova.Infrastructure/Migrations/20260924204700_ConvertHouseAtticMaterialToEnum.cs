using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertHouseAtticMaterialToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // House attic material changed from free text to the AtticMaterial enum. Existing text
            // is mapped by keyword (Romanian/English/Russian); anything unrecognised is dropped —
            // the field is optional, and an unknown string would no longer deserialize.
            migrationBuilder.Sql("""
                UPDATE "Properties" SET "TypeSpecificAttributes" = CASE
                    WHEN mapped IS NULL THEN "TypeSpecificAttributes" - 'atticMaterial'
                    ELSE jsonb_set("TypeSpecificAttributes", '{atticMaterial}', to_jsonb(mapped))
                END
                FROM (
                    SELECT "Id" AS id, CASE
                        WHEN lower(t) IN ('wood', 'drywall', 'osb', 'brick', 'aeratedconcrete', 'other') THEN
                            (ARRAY['Wood', 'Drywall', 'Osb', 'Brick', 'AeratedConcrete', 'Other'])[
                                array_position(ARRAY['wood', 'drywall', 'osb', 'brick', 'aeratedconcrete', 'other'], lower(t))]
                        WHEN t ~* '(lemn|wood|дерев|lambriu)' THEN 'Wood'
                        WHEN t ~* '(gips|drywall|гипс)' THEN 'Drywall'
                        WHEN t ~* 'osb' THEN 'Osb'
                        WHEN t ~* '(c[ăa]r[ăa]mid|brick|кирпич)' THEN 'Brick'
                        WHEN t ~* '(bca|beton celular|aerated|газобетон)' THEN 'AeratedConcrete'
                    END AS mapped
                    FROM (
                        SELECT "Id", "TypeSpecificAttributes"->>'atticMaterial' AS t
                        FROM "Properties"
                        WHERE "PropertyType" = 2 AND "TypeSpecificAttributes" ? 'atticMaterial'
                    ) attic
                ) conversion
                WHERE "Properties"."Id" = conversion.id;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo schema-wise; enum names are valid free text, so old code still reads them.

        }
    }
}
