using System;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// data_render — non-blocking UI panel in Agent Chat (grid | card | chart).
    /// Emits <see cref="AgentDataRenderEvent"/> via <see cref="AgentHitlBridge"/> / OnDataRender,
    /// then returns immediately. Rich payload rides on SSE so MaxToolResultChars cannot truncate it.
    /// </summary>
    public class AgentDataRenderPlugin
    {
        private const int MaxRows = 500;

        public async System.Threading.Tasks.Task<string> Render(
            string ui,
            string dataJson,
            AgentToolContext context,
            string title = null,
            string columnsJson = null,
            string chartConfigJson = null,
            string actionsJson = null,
            string metaJson = null)
        {
            var uiNorm = (ui ?? "").Trim().ToLowerInvariant();
            if (uiNorm != "grid" && uiNorm != "card" && uiNorm != "chart")
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ui must be grid, card, or chart."
                });
            }

            if (string.IsNullOrWhiteSpace(dataJson))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "dataJson is required." });
            }

            string cappedDataJson = dataJson.Trim();
            int rowCount = 0;
            bool truncated = false;

            try
            {
                var token = JToken.Parse(cappedDataJson);
                if (token is JArray arr)
                {
                    rowCount = arr.Count;
                    if (arr.Count > MaxRows)
                    {
                        truncated = true;
                        var slice = new JArray();
                        for (int i = 0; i < MaxRows; i++)
                            slice.Add(arr[i]);
                        cappedDataJson = slice.ToString(Formatting.None);
                        rowCount = MaxRows;
                    }
                }
                else if (token is JObject)
                {
                    rowCount = 1;
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "dataJson must be a JSON array or object."
                    });
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "dataJson is not valid JSON: " + ex.Message
                });
            }

            // Soft-validate optional JSON blobs (keep original if parse fails — UI may still use).
            columnsJson = NormalizeOptionalJson(columnsJson);
            chartConfigJson = NormalizeOptionalJson(chartConfigJson);
            actionsJson = NormalizeOptionalJson(actionsJson);
            metaJson = NormalizeOptionalJson(metaJson);

            var renderId = Guid.NewGuid().ToString("N");
            var evt = new AgentDataRenderEvent
            {
                RenderId = renderId,
                Ui = uiNorm,
                Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                DataJson = cappedDataJson,
                ColumnsJson = columnsJson,
                ChartConfigJson = chartConfigJson,
                ActionsJson = actionsJson,
                MetaJson = metaJson,
                RowCount = rowCount,
                Truncated = truncated,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var onRender = AgentHitlBridge.Current?.OnDataRender;
            if (onRender != null)
            {
                try
                {
                    await onRender(evt).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "data_render callback error: " + ex.Message
                    });
                }
            }

            return JsonConvert.SerializeObject(new
            {
                ok = true,
                renderId,
                ui = uiNorm,
                rowCount,
                truncated,
                pushed = onRender != null
            });
        }

        private static string NormalizeOptionalJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var t = raw.Trim();
            try
            {
                JToken.Parse(t);
                return t;
            }
            catch
            {
                return t;
            }
        }
    }
}
