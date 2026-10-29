-- Single-source formulas for GearVariables.
--
-- NCalc becomes the only hand-maintained formula. The LaTeX shown to the user is generated from it
-- at load time (NCalcLatexRenderer). To make that possible:
--   * Key             - machine identifier of each variable, unique per GearType. NCalc expressions reference
--                       any variable (design or calculated) as [Key]; Symbol is the LaTeX rendering of [Key].
--   * FormulaOverride - the former Formula column. When non-empty it is shown instead of the generated LaTeX.
--                       Keep it only for rows that cannot / should not be generated (e.g. Hand, Newton-Raphson).

ALTER TABLE GearVariables RENAME COLUMN Formula TO FormulaOverride;
ALTER TABLE GearVariables ADD COLUMN "Key" TEXT;

-- Design rows already carry their identifier in NCalc, e.g. '[mn]'.
UPDATE GearVariables
SET "Key" = substr(NCalc, 2, length(NCalc) - 2)
WHERE NCalc GLOB '[[]*[]]' AND NCalc NOT GLOB '*[[]*[]]*[[]*';

-- Calculated rows: alphanumeric characters of the LaTeX symbol, e.g. 'd_{b1}' -> 'db1', '\alpha_{wt}' -> 'alphawt'.
UPDATE GearVariables
SET "Key" = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(trim(Symbol),
            '\', ''), '{', ''), '}', ''), '_', ''), ' ', ''), '^', ''), '''', ''), ':', ''), '+', ''), '-', '')
WHERE "Key" IS NULL;

-- Disambiguate the few symbols that are shared by more than one row of a GearType.
UPDATE GearVariables SET "Key" = 'hand' || substr(GearPart, 5) WHERE VariableId = 'Hand';
UPDATE GearVariables SET "Key" = "Key" || 'NR' WHERE VariableId = 'WorkingPressureAngleNR';

CREATE UNIQUE INDEX GearVariables_Key ON GearVariables (GearType, "Key");
