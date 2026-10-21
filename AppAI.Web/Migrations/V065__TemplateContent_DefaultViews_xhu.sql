-- V065: Default TemplateContent Views
--
-- {{code_default}} means: use the platform default View in C# (UiDefaultViewTemplates),
-- the same role as a compiled Razor/MVC default view — NOT "no template".
-- Full HTML for table/cards lives in APP.BL UiDefaultViewTemplates and is merged with Scriban.
-- Template Editor "Customize" / Reset will persist that HTML into TemplateContent[layout].
--
-- Interactive Result grids use the main app Wijmo (@mescius + license.js), not CDN + key.
-- TemplateContent HTML is for none-interactive Result and export twins.

-- Keep map keys; leave form/kpi/chart/dashboard as code_default until those Views are customized.
-- table/cards already resolve via UiDefaultViewTemplates when marker or missing.

PRINT N'V065: UiDefaultViewTemplates + Scriban bind active in APP.BL; interactive grid = app Wijmo.';
GO
