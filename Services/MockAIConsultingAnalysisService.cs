using System.Diagnostics;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public class MockAIConsultingAnalysisService(
    ApplicationDbContext context,
    IAIContextBuilder contextBuilder,
    IAIProviderClient providerClient,
    IStructuredAIResponseParser parser,
    ILogger<MockAIConsultingAnalysisService> logger) : IAIConsultingAnalysisService
{
    private static readonly string[] ReportSections =
    [
        "Cover / Client Details",
        "Personal Note",
        "AI Readiness Summary",
        "Strengths & Development Areas",
        "AI Readiness Deep-Dive",
        "Competitive Snapshot",
        "Top Recommended AI Use Cases",
        "Recommended Roadmap",
        "Next Steps / How We Can Help"
    ];

    public async Task GenerateCompanySummaryAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var aiContext = await contextBuilder.BuildAsync(new AIContextRequest(clientId, AIOperationNames.CompanySummary));
        var request = new AIProviderRequest(
            AIOperationNames.CompanySummary,
            "You are an AI-assisted consultant. Draft a company summary only from the supplied context. Return valid JSON only.",
            aiContext.PromptText,
            aiContext.ContextText,
            AIJsonSchemas.GetSchemaName(AIOperationNames.CompanySummary),
            AIJsonSchemas.GetSchema(AIOperationNames.CompanySummary));

        var result = await providerClient.GenerateStructuredJsonAsync(request);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Content))
        {
            throw new InvalidOperationException(result.FriendlyMessage ?? "AI could not generate a company summary. Please try again or switch to Mock provider.");
        }

        var parsed = parser.ParseCompanySummary(result.Content);
        var output = $"""
            Summary:
            {parsed.Summary}

            Business model:
            {parsed.BusinessModel ?? "Not specified"}

            Strategic goals:
            {string.Join(Environment.NewLine, parsed.StrategicGoals.Select(goal => $"- {goal}"))}

            Operational context:
            {parsed.OperationalContext ?? "Not specified"}

            AI readiness implications:
            {parsed.AIReadinessImplications ?? "Not specified"}
            """;

        var version = await context.AIAnalysisOutputs
            .Where(item => item.ClientCompanyId == clientId && item.AnalysisType == AnalysisType.CompanySummary)
            .Select(item => (int?)item.VersionNumber)
            .MaxAsync() ?? 0;
        context.AIAnalysisOutputs.Add(new AIAnalysisOutput
        {
            ClientCompanyId = clientId,
            AnalysisType = AnalysisType.CompanySummary,
            Title = "Company summary draft",
            InputSummary = $"{aiContext.Sources.Count} compact source references; provider {result.Provider}.",
            OutputContent = output,
            Status = DraftStatus.DraftGenerated,
            VersionNumber = version + 1,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = result.Provider.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        foreach (var source in parsed.Sources.Concat(aiContext.Sources).Take(30))
        {
            context.AIOutputSources.Add(new AIOutputSource
            {
                ClientCompanyId = clientId,
                OutputType = AIOutputType.CompanySummary,
                SourceType = source.SourceType,
                SourceCategory = source.SourceCategory,
                SourceLabel = source.SourceLabel,
                SourceReference = source.SourceReference,
                SourceUrl = source.SourceUrl,
                EvidenceText = source.EvidenceText,
                CreatedAt = DateTime.UtcNow
            });
        }

        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.CompanySummary;
        client.NextAction = "Review and approve company summary";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Company Summary", WorkflowStepStatus.InProgress);
        AddActivityLog(profile.Id, "Company summary generated", $"Company Summary generated using {result.Provider}.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "CompanySummary", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: 2);
    }

    public async Task GenerateGapAnalysisAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var evidence = await LoadRequiredLatestResponseEvidenceAsync(clientId);
        var existingOpenAreas = await context.GapAnalysisItems
            .AsNoTracking()
            .Where(gap => gap.ClientCompanyId == clientId && gap.Status != GapStatus.Resolved)
            .Select(gap => gap.GapArea)
            .Distinct()
            .ToListAsync();
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var evidenceText = BuildEvidenceText(profile, evidence);
        var candidates = new List<GapAnalysisItem>();

        AddGapIfMissing(candidates, clientId, evidenceText, ["data source", "data sources", "dataset", "crm", "analytics", "warehouse"], GapArea.DataReadiness,
            "Available data sources are unclear or incomplete.",
            "The consultant cannot reliably assess data readiness or pilot feasibility.",
            "Which data sources, owners, and access constraints should the first pilots rely on?",
            "Document source systems, data owners, access rules, and known quality issues.");

        AddGapIfMissing(candidates, clientId, evidenceText, ["process", "workflow", "manual", "handoff", "operation"], GapArea.ProcessReadiness,
            "Manual and repetitive processes have not been described enough.",
            "Use-case generation may miss high-value internal automation opportunities.",
            "Which processes consume the most time or create the most rework?",
            "Map candidate workflows and quantify time spent, volume, and error rates.");

        AddGapIfMissing(candidates, clientId, evidenceText, ["budget", "timeline", "funding", "investment"], GapArea.Budget,
            "Budget or timeline expectations are unclear.",
            "Recommendations may not align with the client's implementation capacity.",
            "What budget range and timeline should the first AI roadmap assume?",
            "Agree budget bands and timeline constraints before prioritizing pilots.");

        AddGapIfMissing(candidates, clientId, evidenceText, ["owner", "sponsor", "accountable", "product owner"], GapArea.Ownership,
            "No AI owner or executive sponsor is clearly identified.",
            "Pilots may stall without accountable decision makers.",
            "Who owns AI prioritization, approvals, and adoption?",
            "Assign an executive sponsor and operational owner for each shortlisted use case.");

        AddGapIfMissing(candidates, clientId, evidenceText, ["governance", "policy", "risk", "compliance", "approval"], GapArea.Governance,
            "AI governance, policy, or risk review is not sufficiently described.",
            "Client-facing or regulated AI use cases may carry unmanaged risk.",
            "What rules govern AI use, model review, privacy, and human oversight?",
            "Create a lightweight AI usage policy before live pilots.");

        var newGaps = candidates
            .Where(gap => !existingOpenAreas.Contains(gap.GapArea))
            .ToList();
        context.GapAnalysisItems.AddRange(newGaps);

        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.GapAnalysis;
        client.NextAction = "Review and clarify open gap items";
        Touch(client);

        await AddAnalysisOutputAsync(clientId, AnalysisType.GapAnalysis, "Gap analysis draft", BuildGapOutput(profile, newGaps), BuildInputSummary(profile, evidence));
        await MarkWorkflowAsync(clientId, "Follow-up Discovery", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Gap analysis generated", "Rule-based gap analysis draft generated from latest assessment evidence.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "GapAnalysis", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: newGaps.Count + 2, recordsUpdated: 2);
    }

    public async Task GenerateSwotAnalysisAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var evidence = await LoadRequiredLatestResponseEvidenceAsync(clientId);
        var existingCategories = await context.SwotAnalysisItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .Select(item => item.Category)
            .Distinct()
            .ToListAsync();
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var created = 0;
        if (!existingCategories.Contains(SwotCategory.Strength))
        {
            context.SwotAnalysisItems.Add(CreateSwot(clientId, SwotCategory.Strength, "Structured readiness workflow and consultant ownership are already in place.", evidence.ResponseLabel));
            created++;
        }

        if (!existingCategories.Contains(SwotCategory.Weakness))
        {
            context.SwotAnalysisItems.Add(CreateSwot(clientId, SwotCategory.Weakness, "Several readiness inputs still need validation before final recommendations.", evidence.ResponseLabel));
            created++;
        }

        if (!existingCategories.Contains(SwotCategory.Opportunity))
        {
            context.SwotAnalysisItems.Add(CreateSwot(clientId, SwotCategory.Opportunity, $"High-value AI opportunities likely exist in {profile.Industry ?? "the client's operating model"}.", "Mock analysis"));
            created++;
        }

        if (!existingCategories.Contains(SwotCategory.Threat))
        {
            context.SwotAnalysisItems.Add(CreateSwot(clientId, SwotCategory.Threat, "Poorly governed pilots could create adoption, privacy, or trust risks.", "Risk review"));
            created++;
        }

        await AddAnalysisOutputAsync(clientId, AnalysisType.Swot, "SWOT draft", $"Draft SWOT generated for {profile.CompanyName}. Review each quadrant and approve before report use.", BuildInputSummary(profile, evidence));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.SwotAnalysis;
        client.NextAction = "Review and approve SWOT draft";
        Touch(client);
        await MarkWorkflowAsync(clientId, "SWOT Analysis", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "SWOT generated", "SWOT draft generated for consultant review.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "SWOT", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: created + 2);
    }

    public async Task GenerateIndustryAnalysisAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        context.IndustryInsights.Add(new IndustryInsight
        {
            ClientCompanyId = clientId,
            Topic = $"{profile.Industry ?? "Industry"} AI adoption themes",
            InsightText = $"Organizations in {profile.Industry ?? "this industry"} are prioritizing internal productivity assistants, document intelligence, decision support, and carefully governed customer-facing automation.",
            Relevance = $"Use as a starting draft for {profile.Country ?? "the client's market"} and replace with sourced research during consultant review.",
            SourceType = InsightSourceType.AiGenerated,
            Status = InsightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        await AddAnalysisOutputAsync(clientId, AnalysisType.IndustryAnalysis, "Industry analysis draft", "Placeholder industry analysis generated. Add sourced consultant research before approving.", BuildInputSummary(profile));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.IndustryAnalysis;
        client.NextAction = "Review and source industry analysis";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Industry Analysis", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Industry analysis generated", "Placeholder industry insight saved.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "IndustryAnalysis", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: 3);
    }

    public async Task GenerateCompetitorInsightsAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        context.CompetitorInsights.Add(new CompetitorInsight
        {
            ClientCompanyId = clientId,
            CompetitorName = "Representative competitor",
            WebsiteUrl = profile.WebsiteUrl,
            InsightText = $"A comparable {profile.Industry ?? "industry"} organization may use AI for customer support, knowledge retrieval, workflow automation, and analytics acceleration.",
            AiUseCasesObserved = "Support assistant; internal knowledge assistant; automated reporting.",
            StrengthComparedToClient = "May move faster if governance and data foundations are already clear.",
            WeaknessComparedToClient = "May lack the client's domain-specific process knowledge.",
            SourceNotes = "Placeholder only; replace with web research or consultant-verified evidence.",
            Status = InsightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        await AddAnalysisOutputAsync(clientId, AnalysisType.CompetitorInsights, "Competitor insight draft", "Placeholder competitor insight generated. Validate sources before approval.", BuildInputSummary(profile));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.CompetitorAnalysis;
        client.NextAction = "Review and source competitor insights";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Competitor Analysis", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Competitor insights generated", "Placeholder competitor insight saved.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "CompetitorInsights", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: 3);
    }

    public async Task GenerateUseCasesAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var evidence = await LoadRequiredLatestResponseEvidenceAsync(clientId);
        var existingTitles = await context.AIUseCases
            .AsNoTracking()
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .Select(useCase => useCase.Title)
            .ToListAsync();
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var created = AddDefaultUseCases(clientId, existingTitles);
        await AddAnalysisOutputAsync(clientId, AnalysisType.UseCaseGeneration, "AI use case suggestions", "Suggested use cases generated from the default consulting library. Shortlist the top three after scoring.", BuildInputSummary(profile, evidence));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.UseCaseIdentification;
        client.NextAction = "Shortlist and approve recommended use cases";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Use Case Identification", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Use cases generated", "Default AI use case library suggestions saved.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "UseCases", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: created + 2);
    }

    public async Task ScoreUseCasesAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var latestScore = await LoadLatestReadinessScoreAsync(clientId);
        var useCases = await context.AIUseCases
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .OrderBy(useCase => useCase.Title)
            .ToListAsync();
        if (useCases.Count == 0)
        {
            throw new InvalidOperationException("Cannot score use cases yet. Please generate AI use cases first.");
        }

        var useCaseIds = useCases.Select(useCase => useCase.Id).ToList();
        var scores = await context.AIUseCaseScores
            .Where(score => useCaseIds.Contains(score.AIUseCaseId))
            .ToDictionaryAsync(score => score.AIUseCaseId);
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var created = 0;
        foreach (var useCase in useCases)
        {
            if (!scores.TryGetValue(useCase.Id, out var score))
            {
                score = new AIUseCaseScore
                {
                    AIUseCaseId = useCase.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.AIUseCaseScores.Add(score);
                created++;
            }

            score.RoiScore = useCase.Title.Contains("report", StringComparison.OrdinalIgnoreCase) ? 5 : 4;
            score.FeasibilityScore = useCase.ImplementationComplexity switch
            {
                ComplexityLevel.Low => 5,
                ComplexityLevel.Medium => 4,
                _ => 3
            };
            score.RiskSafetyScore = useCase.RiskLevel switch
            {
                RiskLevel.Low => 5,
                RiskLevel.Medium => 3,
                _ => 2
            };
            score.StrategicFitScore = latestScore?.OverallScore >= 70 ? 4 : 3;
            score.DataReadinessScore = latestScore is null
                ? 3
                : Math.Clamp((int)Math.Round(latestScore.DataReadinessScore / 20.0), 1, 5);
            score.ScoringComment = "MVP weighted score using ROI, feasibility, strategic fit, data readiness, and risk/safety.";
            score.LastModifiedAt = DateTime.UtcNow;
            score.RecalculatePriority();
        }

        await AddAnalysisOutputAsync(clientId, AnalysisType.UseCaseScoring, "Use case scoring draft", "Use cases scored with the MVP weighted rule. Consultant should adjust scores where client context warrants.", BuildInputSummary(profile, latestScore: latestScore));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.UseCaseScoring;
        client.NextAction = "Review use case scoring and approve roadmap inputs";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Use Case Scoring", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Use cases scored", "Use case priority scores recalculated.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "ScoreUseCases", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: created + 2, recordsUpdated: useCases.Count - created);
    }

    public async Task GenerateReadinessScoreAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var evidence = await LoadRequiredLatestResponseEvidenceAsync(clientId);
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var calculation = CalculateReadinessScore(profile, evidence.Answers);
        var score = new ReadinessScore
        {
            ClientCompanyId = clientId,
            BusinessClarityScore = calculation.BusinessClarityScore,
            DataReadinessScore = calculation.DataReadinessScore,
            ProcessReadinessScore = calculation.ProcessReadinessScore,
            TechnologyReadinessScore = calculation.TechnologyReadinessScore,
            PeopleGovernanceScore = calculation.PeopleGovernanceScore,
            GovernanceComplianceScore = calculation.GovernanceComplianceScore,
            OverallScore = calculation.OverallScore,
            ScoreCategory = calculation.ScoreCategory,
            ScoringSummary = calculation.ScoringSummary,
            CreatedAt = DateTime.UtcNow
        };
        context.ReadinessScores.Add(score);

        var client = await LoadClientForUpdateAsync(clientId);
        client.OverallReadinessScore = calculation.OverallScore;
        client.CurrentStage = ClientStage.ReadinessScore;
        client.NextAction = "Review readiness score rationale";
        Touch(client);

        await AddAnalysisOutputAsync(clientId, AnalysisType.RiskAnalysis, "Readiness score rationale", calculation.ScoringSummary, BuildInputSummary(profile, evidence));
        await MarkWorkflowAsync(clientId, "Readiness Score", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Readiness score calculated", "Readiness score calculated.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "CalculateReadinessScore", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: 3, recordsUpdated: 1, score: calculation.OverallScore);
    }

    public async Task GenerateRoadmapAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var existingTitles = await context.AIUseCases
            .AsNoTracking()
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .Select(useCase => useCase.Title)
            .ToListAsync();
        if (existingTitles.Count == 0)
        {
            _ = await LoadRequiredLatestResponseEvidenceAsync(clientId);
            AddDefaultUseCases(clientId, existingTitles);
            await context.SaveChangesAsync();
        }

        var useCases = await LoadTopUseCasesAsync(clientId, take: 4);
        var gapItems = await context.GapAnalysisItems
            .AsNoTracking()
            .Where(gap => gap.ClientCompanyId == clientId && gap.Status != GapStatus.Resolved)
            .OrderByDescending(gap => gap.Severity)
            .ThenByDescending(gap => gap.CreatedAt)
            .Take(8)
            .Select(gap => new GapSummary(gap.GapArea, gap.IssueDescription, gap.Severity, gap.Status))
            .ToListAsync();
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        if (useCases.Count == 0)
        {
            throw new InvalidOperationException("Cannot generate a roadmap yet. Please generate AI use cases first.");
        }

        var generateStopwatch = Stopwatch.StartNew();
        var phases = new[] { RoadmapPhase.ZeroToThreeMonths, RoadmapPhase.ThreeToSixMonths, RoadmapPhase.SixToTwelveMonths, RoadmapPhase.TwelvePlusMonths };
        var created = 0;
        for (var i = 0; i < useCases.Count; i++)
        {
            var useCase = useCases[i];
            var exists = await context.AIRoadmapItems
                .AnyAsync(item => item.ClientCompanyId == clientId && item.Title == useCase.Title);
            if (exists)
            {
                continue;
            }

            context.AIRoadmapItems.Add(new AIRoadmapItem
            {
                ClientCompanyId = clientId,
                Phase = phases[Math.Min(i, phases.Length - 1)],
                Title = useCase.Title,
                Description = $"Plan and deliver a controlled pilot for {useCase.Title}.",
                RelatedUseCaseId = useCase.Id,
                Owner = useCase.Department,
                ExpectedOutcome = useCase.ExpectedBenefit,
                Dependencies = gapItems.Count > 0 ? "Resolve priority readiness gaps, approve data access, assign owner, and define success metric." : "Approved data access, use-case owner, success metric, and risk review.",
                Status = ApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow
            });
            created++;
        }

        await AddAnalysisOutputAsync(clientId, AnalysisType.Roadmap, "Roadmap draft", "Roadmap generated from top use cases and known readiness gaps.", BuildInputSummary(profile, gapCount: gapItems.Count, useCaseCount: useCases.Count));
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.RoadmapGeneration;
        client.NextAction = "Review and approve roadmap phases";
        Touch(client);
        await MarkWorkflowAsync(clientId, "Roadmap Generation", WorkflowStepStatus.InProgress);
        AddActivityLog(clientId, "Roadmap generated", "Roadmap draft generated from use case priorities.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "Roadmap", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: created + 2);
    }

    public async Task GenerateReportDraftAsync(int clientId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var loadStopwatch = Stopwatch.StartNew();
        var profile = await LoadClientProfileAsync(clientId);
        var evidence = await LoadRequiredLatestResponseEvidenceAsync(clientId);
        var latestScore = await LoadLatestReadinessScoreAsync(clientId);
        var gaps = await LoadGapSummariesAsync(clientId, take: 8);
        var swotItems = await LoadSwotSummariesAsync(clientId, take: 12);
        var industryInsights = await LoadIndustrySummariesAsync(clientId, take: 4);
        var competitorInsights = await LoadCompetitorSummariesAsync(clientId, take: 4);
        var useCases = await LoadTopUseCasesAsync(clientId, take: 6);
        var roadmapItems = await LoadRoadmapSummariesAsync(clientId, take: 8);
        var version = await context.ClientReports
            .Where(report => report.ClientCompanyId == clientId)
            .Select(report => (int?)report.VersionNumber)
            .MaxAsync() ?? 0;
        var loadMs = loadStopwatch.ElapsedMilliseconds;

        var generateStopwatch = Stopwatch.StartNew();
        var reportContext = new ReportDraftContext(profile, evidence, latestScore, gaps, swotItems, industryInsights, competitorInsights, useCases, roadmapItems);
        var report = new ClientReport
        {
            ClientCompanyId = clientId,
            ReportTitle = $"{profile.CompanyName} AI Readiness Report",
            ReportStatus = ReportStatus.DraftGenerated,
            VersionNumber = version + 1,
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            FinalReportContent = $"Draft report for {profile.CompanyName}. Overall score: {latestScore?.OverallScore.ToString() ?? "not generated"}."
        };

        for (var i = 0; i < ReportSections.Length; i++)
        {
            report.Sections.Add(new ReportSection
            {
                SectionTitle = ReportSections[i],
                SectionOrder = i + 1,
                SectionContent = BuildReportSectionDraft(reportContext, ReportSections[i]),
                SectionStatus = SectionStatus.DraftGenerated,
                SourceSummary = "Latest assessment response, generated workspace outputs, and consultant-reviewed evidence.",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.ClientReports.Add(report);
        var client = await LoadClientForUpdateAsync(clientId);
        client.CurrentStage = ClientStage.StrategicReport;
        client.NextAction = "Review and approve report sections";
        Touch(client);

        await AddAnalysisOutputAsync(clientId, AnalysisType.FinalReportSection, "Strategic report draft generated", "A full strategic report draft with editable sections has been generated.", BuildInputSummary(profile, evidence, gaps.Count, useCases.Count, roadmapItems.Count, latestScore));
        await MarkWorkflowAsync(clientId, "Strategic Report", WorkflowStepStatus.Completed);
        AddActivityLog(clientId, "Report draft generated", $"Report version {report.VersionNumber} generated.");
        var generateMs = generateStopwatch.ElapsedMilliseconds;

        var saveMs = await SaveChangesWithTimingAsync();
        LogOperationCompleted(clientId, "ReportDraft", loadMs, generateMs, saveMs, totalStopwatch.ElapsedMilliseconds, recordsCreated: report.Sections.Count + 3, recordsUpdated: 2);
    }

    private async Task<ClientAnalysisProfile> LoadClientProfileAsync(int clientId)
    {
        return await context.ClientCompanies
            .AsNoTracking()
            .Where(client => client.Id == clientId)
            .Select(client => new ClientAnalysisProfile(
                client.Id,
                client.CompanyName,
                client.Industry,
                client.WebsiteUrl,
                client.Country,
                client.Region,
                client.CompanySizeRange,
                client.RevenueRange,
                client.BusinessModel,
                client.KeyRisksSummary,
                client.NextAction,
                client.CurrentStage,
                client.OverallReadinessScore))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Client was not found.");
    }

    private async Task<ClientCompany> LoadClientForUpdateAsync(int clientId)
    {
        return await context.ClientCompanies
            .FirstOrDefaultAsync(client => client.Id == clientId)
            ?? throw new InvalidOperationException("Client was not found.");
    }

    private async Task<AssessmentResponseEvidence> LoadRequiredLatestResponseEvidenceAsync(int clientId)
    {
        return await LoadLatestResponseEvidenceAsync(clientId, loadAnswers: true)
            ?? throw new InvalidOperationException("Cannot calculate or generate analysis yet. Please collect or import an assessment response first.");
    }

    private async Task<AssessmentResponseEvidence?> LoadLatestResponseEvidenceAsync(int clientId, bool loadAnswers)
    {
        var response = await context.AssessmentResponses
            .AsNoTracking()
            .Where(item =>
                item.ReadinessAssessment!.ClientCompanyId == clientId &&
                item.Status != AssessmentResponseStatus.Ignored &&
                item.AnswerCount > 0)
            .OrderByDescending(item => item.ReceivedAt)
            .ThenByDescending(item => item.ResponseNumber)
            .ThenByDescending(item => item.Id)
            .Select(item => new AssessmentResponseEvidence
            {
                Id = item.Id,
                ReadinessAssessmentId = item.ReadinessAssessmentId,
                ResponseNumber = item.ResponseNumber,
                ResponseLabel = item.ResponseLabel,
                Source = item.Source,
                ReceivedAt = item.ReceivedAt,
                AnswerCount = item.AnswerCount,
                Status = item.Status
            })
            .FirstOrDefaultAsync();

        if (response is null)
        {
            return null;
        }

        if (loadAnswers)
        {
            response.Answers = await context.AssessmentAnswers
                .AsNoTracking()
                .Where(answer => answer.AssessmentResponseId == response.Id)
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .Select(answer => new AssessmentAnswerEvidence(
                    answer.SectionName,
                    answer.QuestionText,
                    answer.AnswerText,
                    answer.AnswerType,
                    answer.CompletenessStatus))
                .ToListAsync();
        }

        return response;
    }

    private async Task<ReadinessScore?> LoadLatestReadinessScoreAsync(int clientId)
    {
        return await context.ReadinessScores
            .AsNoTracking()
            .Where(score => score.ClientCompanyId == clientId)
            .OrderByDescending(score => score.CreatedAt)
            .ThenByDescending(score => score.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<List<UseCaseSummary>> LoadTopUseCasesAsync(int clientId, int take)
    {
        var useCases = await context.AIUseCases
            .AsNoTracking()
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .Select(useCase => new UseCaseSummary(
                useCase.Id,
                useCase.Title,
                useCase.Description,
                useCase.Department,
                useCase.ExpectedBenefit,
                useCase.RequiredData,
                useCase.Score == null ? null : useCase.Score.PriorityScore))
            .OrderByDescending(useCase => useCase.PriorityScore ?? 0)
            .ThenBy(useCase => useCase.Title)
            .Take(take)
            .ToListAsync();

        return useCases;
    }

    private async Task<List<GapSummary>> LoadGapSummariesAsync(int clientId, int take)
    {
        return await context.GapAnalysisItems
            .AsNoTracking()
            .Where(gap => gap.ClientCompanyId == clientId)
            .OrderByDescending(gap => gap.Severity)
            .ThenByDescending(gap => gap.CreatedAt)
            .Take(take)
            .Select(gap => new GapSummary(gap.GapArea, gap.IssueDescription, gap.Severity, gap.Status))
            .ToListAsync();
    }

    private async Task<List<SwotSummary>> LoadSwotSummariesAsync(int clientId, int take)
    {
        return await context.SwotAnalysisItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .OrderBy(item => item.Category)
            .ThenByDescending(item => item.CreatedAt)
            .Take(take)
            .Select(item => new SwotSummary(item.Category, item.Description))
            .ToListAsync();
    }

    private async Task<List<IndustrySummary>> LoadIndustrySummariesAsync(int clientId, int take)
    {
        return await context.IndustryInsights
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .Select(item => new IndustrySummary(item.Topic, item.InsightText))
            .ToListAsync();
    }

    private async Task<List<CompetitorSummary>> LoadCompetitorSummariesAsync(int clientId, int take)
    {
        return await context.CompetitorInsights
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .Select(item => new CompetitorSummary(item.CompetitorName, item.InsightText, item.AiUseCasesObserved))
            .ToListAsync();
    }

    private async Task<List<RoadmapSummary>> LoadRoadmapSummariesAsync(int clientId, int take)
    {
        return await context.AIRoadmapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .OrderBy(item => item.Phase)
            .ThenBy(item => item.CreatedAt)
            .Take(take)
            .Select(item => new RoadmapSummary(item.Phase, item.Title, item.ExpectedOutcome))
            .ToListAsync();
    }

    private int AddDefaultUseCases(int clientId, IReadOnlyCollection<string> existingTitles)
    {
        var existing = existingTitles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var useCases = new[]
        {
            ("Internal knowledge assistant", "Answer employee questions from approved company knowledge.", "Operations", ComplexityLevel.Medium, RiskLevel.Low, TimeToValue.ThreeToSixMonths),
            ("Customer support assistant", "Draft responses and route frequent support requests.", "Customer Support", ComplexityLevel.Medium, RiskLevel.Medium, TimeToValue.ThreeToSixMonths),
            ("Automated report generation", "Generate first-draft management and operational reports.", "Operations", ComplexityLevel.Low, RiskLevel.Low, TimeToValue.ZeroToThreeMonths),
            ("Sales proposal assistant", "Draft tailored proposal sections from approved offers and client context.", "Sales", ComplexityLevel.Medium, RiskLevel.Medium, TimeToValue.ThreeToSixMonths),
            ("Meeting summary and action tracker", "Summarize meetings and create follow-up actions.", "Customer Success", ComplexityLevel.Low, RiskLevel.Low, TimeToValue.ZeroToThreeMonths),
            ("Invoice/document processing", "Extract, classify, and reconcile structured fields from documents.", "Finance", ComplexityLevel.Medium, RiskLevel.Medium, TimeToValue.ThreeToSixMonths),
            ("Marketing content assistant", "Create draft campaign copy under brand and review controls.", "Marketing", ComplexityLevel.Low, RiskLevel.Medium, TimeToValue.ZeroToThreeMonths),
            ("HR onboarding assistant", "Help new employees find policies, process guidance, and training paths.", "HR", ComplexityLevel.Low, RiskLevel.Low, TimeToValue.ThreeToSixMonths),
            ("Competitor monitoring assistant", "Summarize competitor changes from approved sources.", "Strategy", ComplexityLevel.Medium, RiskLevel.Medium, TimeToValue.SixToTwelveMonths),
            ("Data quality assistant", "Detect incomplete, inconsistent, or stale business data.", "Data", ComplexityLevel.High, RiskLevel.Medium, TimeToValue.SixToTwelveMonths)
        };

        var created = 0;
        foreach (var useCase in useCases)
        {
            if (existing.Contains(useCase.Item1))
            {
                continue;
            }

            context.AIUseCases.Add(new AIUseCase
            {
                ClientCompanyId = clientId,
                Title = useCase.Item1,
                Description = useCase.Item2,
                BusinessProblem = "Reduce repeated manual effort and improve decision quality.",
                Department = useCase.Item3,
                ExpectedBenefit = "Time savings, faster response, better consistency, and reusable knowledge.",
                RequiredData = "Approved internal knowledge, process records, and usage guardrails.",
                ImplementationComplexity = useCase.Item4,
                RiskLevel = useCase.Item5,
                TimeToValue = useCase.Item6,
                Status = UseCaseStatus.Suggested,
                CreatedAt = DateTime.UtcNow
            });
            created++;
        }

        return created;
    }

    private async Task AddAnalysisOutputAsync(int clientId, AnalysisType type, string title, string content, string inputSummary)
    {
        var version = await context.AIAnalysisOutputs
            .Where(output => output.ClientCompanyId == clientId && output.AnalysisType == type)
            .Select(output => (int?)output.VersionNumber)
            .MaxAsync() ?? 0;

        var output = new AIAnalysisOutput
        {
            ClientCompanyId = clientId,
            AnalysisType = type,
            Title = title,
            InputSummary = inputSummary,
            OutputContent = content,
            Status = DraftStatus.DraftGenerated,
            VersionNumber = version + 1,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "MockAI",
            CreatedAt = DateTime.UtcNow
        };
        context.AIAnalysisOutputs.Add(output);
        context.AIOutputSources.Add(new AIOutputSource
        {
            ClientCompanyId = clientId,
            OutputType = MapAIOutputType(type),
            SourceType = AIOutputSourceType.Internal,
            SourceCategory = AIOutputSourceCategory.Other,
            SourceLabel = "Workspace evidence summary",
            SourceReference = title,
            EvidenceText = inputSummary,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static AIOutputType MapAIOutputType(AnalysisType type)
    {
        return type switch
        {
            AnalysisType.CompanySummary => AIOutputType.CompanySummary,
            AnalysisType.KnowledgeGapAnalysis => AIOutputType.KnowledgeGap,
            AnalysisType.Swot => AIOutputType.SWOT,
            AnalysisType.IndustryAnalysis => AIOutputType.IndustryAnalysis,
            AnalysisType.CompetitorInsights => AIOutputType.CompetitorAnalysis,
            AnalysisType.UseCaseGeneration => AIOutputType.UseCase,
            AnalysisType.UseCaseScoring => AIOutputType.UseCaseScore,
            AnalysisType.Roadmap => AIOutputType.Roadmap,
            AnalysisType.FinalReportSection or AnalysisType.ExecutiveSummary => AIOutputType.Report,
            AnalysisType.RiskAnalysis => AIOutputType.ReadinessScore,
            _ => AIOutputType.CompanySummary
        };
    }

    private static string BuildInputSummary(
        ClientAnalysisProfile profile,
        AssessmentResponseEvidence? evidence = null,
        int gapCount = 0,
        int useCaseCount = 0,
        int roadmapCount = 0,
        ReadinessScore? latestScore = null)
    {
        var responseText = evidence is null
            ? "no selected response"
            : $"{evidence.AnswerCount} answers from {evidence.ResponseLabel}";
        var scoreText = latestScore is null ? "no readiness score" : $"score {latestScore.OverallScore}";
        return $"{profile.CompanyName}; {profile.Industry ?? "industry not set"}; {responseText}; {scoreText}; {gapCount} gaps; {useCaseCount} use cases; {roadmapCount} roadmap items.";
    }

    private static string BuildEvidenceText(ClientAnalysisProfile profile, AssessmentResponseEvidence evidence)
    {
        var values = new List<string?>
        {
            profile.CompanyName,
            profile.Industry,
            profile.BusinessModel,
            profile.KeyRisksSummary,
            profile.NextAction
        };
        values.AddRange(evidence.Answers.Select(answer => $"{answer.QuestionText} {answer.AnswerText} {answer.SectionName}"));
        return string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
    }

    private static ReadinessScoreCalculation CalculateReadinessScore(ClientAnalysisProfile profile, IReadOnlyList<AssessmentAnswerEvidence> answers)
    {
        var business = CalculateCategoryScore(profile, answers, ["strategy", "objective", "business model", "revenue", "differentiator", "goal", "pain point"]);
        var data = CalculateCategoryScore(profile, answers, ["data", "quality", "system", "reporting", "integration", "crm", "warehouse", "source"]);
        var technology = CalculateCategoryScore(profile, answers, ["tool", "cloud", "saas", "automation", "technical", "integration", "platform", "software"]);
        var process = CalculateCategoryScore(profile, answers, ["process", "workflow", "standard", "repeat", "operation", "bottleneck", "manual", "handoff"]);
        var people = CalculateCategoryScore(profile, answers, ["skill", "owner", "leadership", "change", "training", "adoption", "team", "sponsor"]);
        var governance = CalculateCategoryScore(profile, answers, ["risk", "compliance", "governance", "policy", "privacy", "approval", "security", "legal"]);
        var overall = (int)Math.Round((business + data + technology + process + people + governance) / 6.0);
        var category = GetScoreCategory(overall);

        var categoryScores = new Dictionary<string, int>
        {
            ["Business clarity"] = business,
            ["Data readiness"] = data,
            ["Technology readiness"] = technology,
            ["Process readiness"] = process,
            ["People and change"] = people,
            ["Governance / compliance"] = governance
        };
        var strongest = categoryScores.OrderByDescending(item => item.Value).First();
        var weakest = categoryScores.OrderBy(item => item.Value).First();
        var missingAnswers = answers.Count(answer => IsMissing(answer));
        var categoryLabel = GetScoreCategoryLabel(category);

        var summary = $"{categoryLabel} readiness based on {answers.Count} answers from the latest assessment response. Overall: {overall}/100 ({overall / 10m:0.0}/10). Strongest area: {strongest.Key} ({strongest.Value}/100). Needs attention: {weakest.Key} ({weakest.Value}/100). {missingAnswers} missing or weak answer{(missingAnswers == 1 ? "" : "s")} reduced the score.";

        return new ReadinessScoreCalculation(business, data, technology, process, people, governance, overall, category, summary);
    }

    private static int CalculateCategoryScore(ClientAnalysisProfile profile, IReadOnlyList<AssessmentAnswerEvidence> answers, string[] keywords)
    {
        var relevantAnswers = answers
            .Where(answer => ContainsAny($"{answer.SectionName} {answer.QuestionText} {answer.AnswerText}", keywords))
            .ToList();

        if (relevantAnswers.Count == 0)
        {
            relevantAnswers = answers.ToList();
        }

        if (relevantAnswers.Count == 0)
        {
            return 25;
        }

        var completeAnswers = relevantAnswers.Count(answer => !IsMissing(answer));
        var detailedAnswers = relevantAnswers.Count(answer => (answer.AnswerText?.Trim().Length ?? 0) >= 80);
        var strongSignals = relevantAnswers.Count(answer => ContainsAny(answer.AnswerText ?? string.Empty, ["clear", "defined", "owner", "available", "documented", "measured", "standard", "approved", "cloud", "integrated"]));
        var weakSignals = relevantAnswers.Count(answer => ContainsAny(answer.AnswerText ?? string.Empty, ["unknown", "none", "missing", "not sure", "unclear", "manual", "ad hoc", "spreadsheet"]));

        var completenessScore = (double)completeAnswers / relevantAnswers.Count * 45;
        var detailScore = (double)detailedAnswers / relevantAnswers.Count * 20;
        var signalScore = Math.Min(strongSignals * 4, 15);
        var weakPenalty = Math.Min(weakSignals * 5, 20);
        var profileBoost = string.IsNullOrWhiteSpace(profile.BusinessModel) ? 0 : 5;

        return Clamp((int)Math.Round(25 + completenessScore + detailScore + signalScore + profileBoost - weakPenalty));
    }

    private static bool IsMissing(AssessmentAnswerEvidence answer)
    {
        return string.IsNullOrWhiteSpace(answer.AnswerText) ||
            answer.CompletenessStatus == CompletenessStatus.Missing;
    }

    private static bool ContainsAny(string text, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddGapIfMissing(List<GapAnalysisItem> gaps, int clientId, string evidenceText, string[] keywords, GapArea area, string issue, string impact, string question, string action)
    {
        if (keywords.Any(keyword => evidenceText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        gaps.Add(new GapAnalysisItem
        {
            ClientCompanyId = clientId,
            GapArea = area,
            IssueDescription = issue,
            Impact = impact,
            Severity = area is GapArea.Governance or GapArea.DataReadiness ? Severity.High : Severity.Medium,
            SuggestedFollowUpQuestion = question,
            SuggestedAction = action,
            Status = GapStatus.Open,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildGapOutput(ClientAnalysisProfile profile, IReadOnlyCollection<GapAnalysisItem> gaps)
    {
        if (gaps.Count == 0)
        {
            return $"No new rule-based gaps were detected for {profile.CompanyName}. Consultant should still review completeness manually.";
        }

        return string.Join(Environment.NewLine, gaps.Select(gap => $"- {gap.GapArea}: {gap.IssueDescription} Suggested action: {gap.SuggestedAction}"));
    }

    private static SwotAnalysisItem CreateSwot(int clientId, SwotCategory category, string description, string evidenceSource)
    {
        return new SwotAnalysisItem
        {
            ClientCompanyId = clientId,
            Category = category,
            Description = description,
            EvidenceSource = evidenceSource,
            Status = ItemReviewStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static int Clamp(int score)
    {
        return Math.Clamp(score, 0, 100);
    }

    private static ScoreCategory GetScoreCategory(int score)
    {
        return score switch
        {
            <= 39 => ScoreCategory.Observer,
            <= 69 => ScoreCategory.CautiousAdopter,
            _ => ScoreCategory.Leader
        };
    }

    private static string GetScoreCategoryLabel(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Observer => "Observer",
            ScoreCategory.CautiousAdopter => "Cautious adopter",
            ScoreCategory.Leader => "Leader",
            ScoreCategory.AiBeginner => "AI beginner",
            ScoreCategory.ExplorationReady => "Exploration ready",
            ScoreCategory.PilotReady => "Pilot ready",
            ScoreCategory.ImplementationReady => "Implementation ready",
            _ => category.ToString()
        };
    }

    private static string BuildReportSectionDraft(ReportDraftContext context, string sectionTitle)
    {
        return sectionTitle switch
        {
            "Cover / Client Details" => $"{context.Profile.CompanyName}{Environment.NewLine}Industry: {context.Profile.Industry ?? "Not specified"}{Environment.NewLine}Prepared for consultant review. Assessment source: {context.Evidence.ResponseLabel}.",
            "Personal Note" => "Consultant note placeholder. Add relationship context, why this assessment matters now, and any client-specific nuance before approval.",
            "AI Readiness Summary" => context.LatestScore is null ? "Readiness score has not been generated yet." : $"Overall score: {context.LatestScore.OverallScore}/100 ({context.LatestScore.OverallScore / 10m:0.0}/10). Adoption profile: {GetScoreCategoryLabel(context.LatestScore.ScoreCategory)}. {context.LatestScore.ScoringSummary}",
            "Strengths & Development Areas" => context.Gaps.Count > 0 ? string.Join(Environment.NewLine, context.Gaps.Take(5).Select(gap => $"- Development area: {gap.GapArea}: {gap.IssueDescription}")) : "No development areas have been captured yet. Add consultant interpretation before approval.",
            "AI Readiness Deep-Dive" => $"{context.Evidence.ResponseLabel} contains {context.Evidence.AnswerCount} assessment answers collected on {context.Evidence.ReceivedAt:yyyy-MM-dd}. Deep-dive should explain strategic context, data readiness, operating bottlenecks, and governance/compliance.",
            "Competitive Snapshot" => BuildCompetitiveSnapshot(context),
            "Top Recommended AI Use Cases" => context.UseCases.Count > 0 ? string.Join(Environment.NewLine, context.UseCases.Take(5).Select(useCase => $"- {useCase.Title}: {useCase.Description}")) : "No AI use cases have been generated yet.",
            "Recommended Roadmap" => context.RoadmapItems.Count > 0 ? string.Join(Environment.NewLine, context.RoadmapItems.Take(5).Select(item => $"- {item.Phase}: {item.Title}")) : "Roadmap has not been generated yet.",
            "Next Steps / How We Can Help" => context.Profile.NextAction ?? "Confirm readiness score, shortlist use cases, validate roadmap owners, and agree the first implementation support step.",
            _ => $"Draft {sectionTitle.ToLowerInvariant()} content for {context.Profile.CompanyName}. Consultant review required before approval."
        };
    }

    private static string BuildCompetitiveSnapshot(ReportDraftContext context)
    {
        var industry = context.IndustryInsights.Count > 0
            ? string.Join(Environment.NewLine, context.IndustryInsights.Select(item => $"- Industry: {item.Topic}: {item.InsightText}"))
            : "- Industry: no sourced industry insight draft available yet.";
        var competitors = context.CompetitorInsights.Count > 0
            ? string.Join(Environment.NewLine, context.CompetitorInsights.Select(item => $"- Competitor: {item.CompetitorName}: {item.InsightText}"))
            : "- Competitors: no competitor insight draft available yet.";

        return $"{industry}{Environment.NewLine}{competitors}";
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var step = await context.ClientWorkflowSteps
            .Where(item => item.ClientCompanyId == clientId && item.StageName == stageName)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync();
        if (step is null)
        {
            context.ClientWorkflowSteps.Add(new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = stageName,
                DisplayOrder = StakeholderWorkflow.GetDisplayOrder(stageName),
                Status = status,
                CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : null
            });
            return;
        }

        step.Status = status;
        step.CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : step.CompletedAt;
    }

    private void AddActivityLog(int clientId, string activityType, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = activityType,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        });
    }

    private async Task<long> SaveChangesWithTimingAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        await context.SaveChangesAsync();
        return stopwatch.ElapsedMilliseconds;
    }

    private void LogOperationCompleted(
        int clientId,
        string operation,
        long loadMs,
        long generateMs,
        long saveMs,
        long totalMs,
        int recordsCreated,
        int recordsUpdated = 0,
        int? score = null)
    {
        logger.LogInformation(
            "Analysis operation completed. ClientCompanyId: {ClientCompanyId}; Operation: {Operation}; LoadContextMs: {LoadContextMs}; GenerateMs: {GenerateMs}; SaveMs: {SaveMs}; TotalMs: {TotalMs}; RecordsCreated: {RecordsCreated}; RecordsUpdated: {RecordsUpdated}; Score: {Score}",
            clientId,
            operation,
            loadMs,
            generateMs,
            saveMs,
            totalMs,
            recordsCreated,
            recordsUpdated,
            score);
    }

    private static void Touch(ClientCompany client)
    {
        client.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedBy = "System";
    }

    private sealed record ClientAnalysisProfile(
        int Id,
        string CompanyName,
        string? Industry,
        string? WebsiteUrl,
        string? Country,
        string? Region,
        string? CompanySizeRange,
        string? RevenueRange,
        string? BusinessModel,
        string? KeyRisksSummary,
        string? NextAction,
        ClientStage CurrentStage,
        decimal? OverallReadinessScore);

    private sealed class AssessmentResponseEvidence
    {
        public int Id { get; init; }
        public int ReadinessAssessmentId { get; init; }
        public int ResponseNumber { get; init; }
        public string ResponseLabel { get; init; } = string.Empty;
        public AssessmentResponseSource Source { get; init; }
        public DateTime ReceivedAt { get; init; }
        public int AnswerCount { get; init; }
        public AssessmentResponseStatus Status { get; init; }
        public IReadOnlyList<AssessmentAnswerEvidence> Answers { get; set; } = [];
    }

    private sealed record AssessmentAnswerEvidence(
        string SectionName,
        string QuestionText,
        string? AnswerText,
        string? AnswerType,
        CompletenessStatus CompletenessStatus);

    private sealed record ReadinessScoreCalculation(
        int BusinessClarityScore,
        int DataReadinessScore,
        int TechnologyReadinessScore,
        int ProcessReadinessScore,
        int PeopleGovernanceScore,
        int GovernanceComplianceScore,
        int OverallScore,
        ScoreCategory ScoreCategory,
        string ScoringSummary);

    private sealed record UseCaseSummary(
        int Id,
        string Title,
        string Description,
        string? Department,
        string? ExpectedBenefit,
        string? RequiredData,
        decimal? PriorityScore);

    private sealed record GapSummary(GapArea GapArea, string IssueDescription, Severity Severity, GapStatus Status);
    private sealed record SwotSummary(SwotCategory Category, string Description);
    private sealed record IndustrySummary(string Topic, string InsightText);
    private sealed record CompetitorSummary(string CompetitorName, string InsightText, string? AiUseCasesObserved);
    private sealed record RoadmapSummary(RoadmapPhase Phase, string Title, string? ExpectedOutcome);

    private sealed record ReportDraftContext(
        ClientAnalysisProfile Profile,
        AssessmentResponseEvidence Evidence,
        ReadinessScore? LatestScore,
        IReadOnlyList<GapSummary> Gaps,
        IReadOnlyList<SwotSummary> SwotItems,
        IReadOnlyList<IndustrySummary> IndustryInsights,
        IReadOnlyList<CompetitorSummary> CompetitorInsights,
        IReadOnlyList<UseCaseSummary> UseCases,
        IReadOnlyList<RoadmapSummary> RoadmapItems);
}
