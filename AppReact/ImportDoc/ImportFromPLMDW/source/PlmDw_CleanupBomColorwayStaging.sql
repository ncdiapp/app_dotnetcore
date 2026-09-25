-- RETIRED. Do not generate or execute 6_PlmDw_CleanupBomColorwayStaging.sql.
-- Fresh imports never create host Colorway_N / ImageN columns.
-- Phase D ApplyDwBlueprintStagingCleanup already removes residual TX staging fields.
-- Kept only so old sessions do not fail file_list if this template is still in source/.
PRINT N'Skipped retired 6_PlmDw_CleanupBomColorwayStaging.';
