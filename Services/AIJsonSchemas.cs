using System.Text.Json;

namespace AI_Readiness_Hub.Services;

public static class AIJsonSchemas
{
    public static string GetSchemaName(string operationName)
    {
        return operationName switch
        {
            AIOperationNames.KnowledgeGapAnalysis => "knowledge_gap_analysis",
            AIOperationNames.CompanySummary => "company_summary",
            AIOperationNames.ReadinessScore => "readiness_score",
            AIOperationNames.IndustryAnalysis => "industry_analysis",
            AIOperationNames.CompetitorAnalysis => "competitor_analysis",
            AIOperationNames.SwotAnalysis => "swot_analysis",
            AIOperationNames.UseCaseIdentification => "use_case_identification",
            AIOperationNames.UseCaseScoring => "use_case_scoring",
            AIOperationNames.RoadmapGeneration => "roadmap_generation",
            AIOperationNames.StrategicReportGeneration => "strategic_report_generation",
            AIOperationNames.AIWorkspaceRefinement => "ai_workspace_refinement",
            _ => "ai_consulting_output"
        };
    }

    public static JsonElement GetSchema(string operationName)
    {
        var schema = operationName switch
        {
            AIOperationNames.KnowledgeGapAnalysis => KnowledgeGapSchema,
            AIOperationNames.CompanySummary => CompanySummarySchema,
            AIOperationNames.AIWorkspaceRefinement => RefinementSchema,
            AIOperationNames.ReadinessScore => ReadinessScoreSchema,
            AIOperationNames.IndustryAnalysis => IndustryAnalysisSchema,
            AIOperationNames.CompetitorAnalysis => CompetitorAnalysisSchema,
            AIOperationNames.SwotAnalysis => SwotSchema,
            AIOperationNames.UseCaseIdentification => UseCaseIdentificationSchema,
            AIOperationNames.UseCaseScoring => UseCaseScoringSchema,
            AIOperationNames.RoadmapGeneration => RoadmapSchema,
            AIOperationNames.StrategicReportGeneration => StrategicReportSchema,
            _ => GenericDraftSchema
        };

        using var document = JsonDocument.Parse(schema);
        return document.RootElement.Clone();
    }

    private const string SourceSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "additionalProperties": false,
            "properties": {
              "sourceType": { "type": "string" },
              "sourceCategory": { "type": "string" },
              "sourceLabel": { "type": "string" },
              "sourceReference": { "type": ["string", "null"] },
              "sourceUrl": { "type": ["string", "null"] },
              "evidenceText": { "type": ["string", "null"] }
            },
            "required": ["sourceType", "sourceCategory", "sourceLabel", "sourceReference", "sourceUrl", "evidenceText"]
          }
        }
        """;

    private static readonly string KnowledgeGapSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "items": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "gapArea": { "type": "string" },
                  "missingInformation": { "type": "string" },
                  "whyItMatters": { "type": "string" },
                  "followUpQuestion": { "type": "string" },
                  "suggestedEvidence": { "type": "string" },
                  "priority": { "type": "string" },
                  "sources": {{SourceSchema}}
                },
                "required": ["gapArea", "missingInformation", "whyItMatters", "followUpQuestion", "suggestedEvidence", "priority", "sources"]
              }
            }
          },
          "required": ["items"]
        }
        """;

    private static readonly string CompanySummarySchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "summary": { "type": "string" },
            "businessModel": { "type": "string" },
            "strategicGoals": { "type": "array", "items": { "type": "string" } },
            "operationalContext": { "type": "string" },
            "aiReadinessImplications": { "type": "string" },
            "sources": {{SourceSchema}}
          },
          "required": ["summary", "businessModel", "strategicGoals", "operationalContext", "aiReadinessImplications", "sources"]
        }
        """;

    private static readonly string RefinementSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "improvedDraft": { "type": "string" },
            "summaryOfChanges": { "type": "string" },
            "sources": {{SourceSchema}}
          },
          "required": ["improvedDraft", "summaryOfChanges", "sources"]
        }
        """;

    private static readonly string ReadinessScoreSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "overallScoreOutOf100": { "type": "number" },
            "overallScoreOutOf10": { "type": "number" },
            "adoptionProfile": { "type": "string" },
            "benchmark": { "type": "string" },
            "interpretation": { "type": "string" },
            "dimensions": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "area": { "type": "string" },
                  "scoreOutOf5": { "type": "number" },
                  "status": { "type": "string" },
                  "interpretation": { "type": "string" }
                },
                "required": ["area", "scoreOutOf5", "status", "interpretation"]
              }
            },
            "sources": {{SourceSchema}}
          },
          "required": ["overallScoreOutOf100", "overallScoreOutOf10", "adoptionProfile", "benchmark", "interpretation", "dimensions", "sources"]
        }
        """;

    private static readonly string IndustryAnalysisSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "industryOverview": { "type": "string" },
            "marketTrends": { "type": "array", "items": { "type": "string" } },
            "aiOpportunities": { "type": "array", "items": { "type": "string" } },
            "risksAndConstraints": { "type": "array", "items": { "type": "string" } },
            "strategicImplications": { "type": "string" },
            "sources": {{SourceSchema}}
          },
          "required": ["industryOverview", "marketTrends", "aiOpportunities", "risksAndConstraints", "strategicImplications", "sources"]
        }
        """;

    private static readonly string CompetitorAnalysisSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "competitors": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "name": { "type": "string" },
                  "positioning": { "type": "string" },
                  "aiActivities": { "type": "string" },
                  "relevanceToClient": { "type": "string" }
                },
                "required": ["name", "positioning", "aiActivities", "relevanceToClient"]
              }
            },
            "competitiveTakeaway": { "type": "string" },
            "sources": {{SourceSchema}}
          },
          "required": ["competitors", "competitiveTakeaway", "sources"]
        }
        """;

    private static readonly string SwotSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "strengths": { "type": "array", "items": { "type": "string" } },
            "weaknesses": { "type": "array", "items": { "type": "string" } },
            "opportunities": { "type": "array", "items": { "type": "string" } },
            "threats": { "type": "array", "items": { "type": "string" } },
            "strategicTakeaway": { "type": "string" },
            "sources": {{SourceSchema}}
          },
          "required": ["strengths", "weaknesses", "opportunities", "threats", "strategicTakeaway", "sources"]
        }
        """;

    private static readonly string UseCaseIdentificationSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "useCases": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "name": { "type": "string" },
                  "description": { "type": "string" },
                  "focus": { "type": "string" },
                  "roiPotential": { "type": "string" },
                  "complexity": { "type": "string" },
                  "dependencies": { "type": "array", "items": { "type": "string" } },
                  "expectedOutcome": { "type": "string" },
                  "sources": {{SourceSchema}}
                },
                "required": ["name", "description", "focus", "roiPotential", "complexity", "dependencies", "expectedOutcome", "sources"]
              }
            }
          },
          "required": ["useCases"]
        }
        """;

    private const string UseCaseScoringSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "scores": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "name": { "type": "string" },
                  "roiScore": { "type": "number" },
                  "feasibilityScore": { "type": "number" },
                  "strategicFitScore": { "type": "number" },
                  "dataReadinessScore": { "type": "number" },
                  "riskSafetyScore": { "type": "number" },
                  "rationale": { "type": "string" }
                },
                "required": ["name", "roiScore", "feasibilityScore", "strategicFitScore", "dataReadinessScore", "riskSafetyScore", "rationale"]
              }
            }
          },
          "required": ["scores"]
        }
        """;

    private static readonly string RoadmapSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "phases": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "phaseName": { "type": "string" },
                  "timeframe": { "type": "string" },
                  "initiatives": { "type": "array", "items": { "type": "string" } },
                  "successCriteria": { "type": "array", "items": { "type": "string" } },
                  "dependencies": { "type": "array", "items": { "type": "string" } }
                },
                "required": ["phaseName", "timeframe", "initiatives", "successCriteria", "dependencies"]
              }
            },
            "sources": {{SourceSchema}}
          },
          "required": ["phases", "sources"]
        }
        """;

    private static readonly string StrategicReportSchema = $$"""
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "sections": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "title": { "type": "string" },
                  "content": { "type": "string" },
                  "sources": {{SourceSchema}}
                },
                "required": ["title", "content", "sources"]
              }
            }
          },
          "required": ["sections"]
        }
        """;

    private const string GenericDraftSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "draft": { "type": "string" },
            "sources": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["draft", "sources"]
        }
        """;
}
