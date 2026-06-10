namespace AI_Readiness_Hub.Models;

public static class StakeholderWorkflow
{
    public static readonly string[] Stages =
    [
        "Client Created",
        "Assessment Sent",
        "Assessment Completed",
        "Knowledge Gap Analysis",
        "Follow-up Discovery",
        "Evidence Collected",
        "Company Summary",
        "Readiness Score",
        "Industry Analysis",
        "Competitor Analysis",
        "SWOT Analysis",
        "Use Case Identification",
        "Use Case Scoring",
        "Roadmap Generation",
        "Strategic Report",
        "Final Review / Approved"
    ];

    public static int GetDisplayOrder(string stageName)
    {
        var normalized = MapLegacyStageName(stageName);
        var index = Array.FindIndex(Stages, stage => stage.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : Stages.Length + 1;
    }

    public static string MapLegacyStageName(string stageName)
    {
        return stageName switch
        {
            "Client Registered" => "Client Created",
            "Readiness Form Sent" => "Assessment Sent",
            "Form Completed" => "Assessment Completed",
            "Documents Uploaded" => "Evidence Collected",
            "Initial AI Analysis Completed" => "Knowledge Gap Analysis",
            "Gap Analysis Completed" => "Follow-up Discovery",
            "Consultant Session Completed" => "Evidence Collected",
            "Report Draft Generated" => "Strategic Report",
            "Consultant Review Completed" => "Final Review / Approved",
            "Final Report Delivered" => "Final Review / Approved",
            _ => stageName
        };
    }
}
