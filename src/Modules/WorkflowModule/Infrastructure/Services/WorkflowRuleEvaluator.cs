using System;
using System.Text.Json;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

public class TriggerRule
{
    public string TelemetryKey { get; set; } = null!;
    public string Operator { get; set; } = ">"; // ">", "<", "==", "!="
    public double? StaticThreshold { get; set; }
    public string? MetadataThresholdKey { get; set; } // Örn: "temperatureLimit"
}

public static class WorkflowRuleEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool Evaluate(double telemetryValue, string? metadataJson, TriggerRule rule)
    {
        double threshold;

        if (rule.StaticThreshold.HasValue)
        {
            threshold = rule.StaticThreshold.Value;
        }
        else if (!string.IsNullOrEmpty(rule.MetadataThresholdKey) && !string.IsNullOrEmpty(metadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                if (doc.RootElement.TryGetProperty(rule.MetadataThresholdKey, out var prop) && prop.TryGetDouble(out var val))
                {
                    threshold = val;
                }
                else
                {
                    // Metadata anahtarı bulunamazsa tetikleme gerçekleşmez.
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return rule.Operator switch
        {
            ">" => telemetryValue > threshold,
            "<" => telemetryValue < threshold,
            "==" => Math.Abs(telemetryValue - threshold) < 0.0001,
            "!=" => Math.Abs(telemetryValue - threshold) >= 0.0001,
            _ => false
        };
    }
}
