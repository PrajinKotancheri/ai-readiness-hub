using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public class MockAIConsultingAnalysisService(ApplicationDbContext context) : IAIConsultingAnalysisService
{
    private static readonly string[] ReportSections =
    [
        "Executive Summary",
        "Company Context",
        "AI Readiness Score",
        "Current State",
        "Gap Analysis",
        "SWOT Analysis",
        "Industry Trends",
        "Competitor Insights",
        "Recommended AI Use Cases",
        "Use Case Scoring",
        "1-Year Roadmap",
        "Risks and Mitigation",
        "Recommended Next Steps"
    ];

    public async Task GenerateCompanySummaryAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        var assessment = client.ReadinessAssessments.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        var output = $"""
            Company: {client.CompanyName}
            Industry: {client.Industry ?? "Not specified"}
            Business model: {client.BusinessModel ?? "Not specified"}
            Current stage: {client.CurrentStage}

            Draft summary:
            {client.CompanyName} is being assessed for practical AI readiness across business clarity, data quality, process maturity, technology fit, and governance. The current assessment suggests {assessment?.Summary ?? "the consultant should validate the client context and import assessment answers before final review."}

            Consultant review notes:
            - Confirm the core business goals.
            - Validate available data sources and ownership.
            - Identify the first safe, measurable pilot candidate.
            """;

        await AddAnalysisOutputAsync(client, AnalysisType.CompanySummary, "Company summary draft", output);
        await LogAsync(clientId, "Company summary generated", "Mock company summary draft saved for consultant review.");
    }

    public async Task GenerateGapAnalysisAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        var text = BuildEvidenceText(client);
        var gaps = new List<GapAnalysisItem>();

        AddGapIfMissing(gaps, clientId, text, ["data source", "data sources", "dataset", "crm", "analytics", "warehouse"], GapArea.DataReadiness,
            "Available data sources are unclear or incomplete.",
            "The consultant cannot reliably assess data readiness or pilot feasibility.",
            "Which data sources, owners, and access constraints should the first pilots rely on?",
            "Document source systems, data owners, access rules, and known quality issues.");

        AddGapIfMissing(gaps, clientId, text, ["process", "workflow", "manual", "handoff", "operation"], GapArea.ProcessReadiness,
            "Manual and repetitive processes have not been described enough.",
            "Use-case generation may miss high-value internal automation opportunities.",
            "Which processes consume the most time or create the most rework?",
            "Map candidate workflows and quantify time spent, volume, and error rates.");

        AddGapIfMissing(gaps, clientId, text, ["budget", "timeline", "funding", "investment"], GapArea.Budget,
            "Budget or timeline expectations are unclear.",
            "Recommendations may not align with the client's implementation capacity.",
            "What budget range and timeline should the first AI roadmap assume?",
            "Agree budget bands and timeline constraints before prioritizing pilots.");

        AddGapIfMissing(gaps, clientId, text, ["owner", "sponsor", "accountable", "product owner"], GapArea.Ownership,
            "No AI owner or executive sponsor is clearly identified.",
            "Pilots may stall without accountable decision makers.",
            "Who owns AI prioritization, approvals, and adoption?",
            "Assign an executive sponsor and operational owner for each shortlisted use case.");

        AddGapIfMissing(gaps, clientId, text, ["governance", "policy", "risk", "compliance", "approval"], GapArea.Governance,
            "AI governance, policy, or risk review is not sufficiently described.",
            "Client-facing or regulated AI use cases may carry unmanaged risk.",
            "What rules govern AI use, model review, privacy, and human oversight?",
            "Create a lightweight AI usage policy before live pilots.");

        foreach (var gap in gaps)
        {
            var exists = await context.GapAnalysisItems.AnyAsync(existing =>
                existing.ClientCompanyId == clientId &&
                existing.GapArea == gap.GapArea &&
                existing.Status != GapStatus.Resolved);
            if (!exists)
            {
                context.GapAnalysisItems.Add(gap);
            }
        }

        await AddAnalysisOutputAsync(client, AnalysisType.GapAnalysis, "Gap analysis draft", BuildGapOutput(client, gaps));
        client.CurrentStage = ClientStage.GapAnalysis;
        client.NextAction = "Review and clarify open gap items";
        Touch(client);
        await context.SaveChangesAsync();
        await MarkWorkflowAsync(clientId, "Gap Analysis Completed", WorkflowStepStatus.InProgress);
        await LogAsync(clientId, "Gap analysis generated", "Rule-based gap analysis draft generated from available evidence.");
    }

    public async Task GenerateSwotAnalysisAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        if (!client.SwotItems.Any())
        {
            context.SwotAnalysisItems.AddRange(
                CreateSwot(clientId, SwotCategory.Strength, "Clear consultant ownership and structured readiness workflow in progress.", "Client workspace"),
                CreateSwot(clientId, SwotCategory.Weakness, "Several readiness inputs still need validation before final recommendations.", "Assessment and gap analysis"),
                CreateSwot(clientId, SwotCategory.Opportunity, $"High-value AI opportunities likely exist in {client.Industry ?? "the client's operating model"}.", "Mock analysis"),
                CreateSwot(clientId, SwotCategory.Threat, "Poorly governed pilots could create adoption, privacy, or trust risks.", "Risk review"));
        }

        await AddAnalysisOutputAsync(client, AnalysisType.Swot, "SWOT draft", $"Draft SWOT generated for {client.CompanyName}. Review each quadrant and approve before report use.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "SWOT generated", "SWOT draft generated for consultant review.");
    }

    public async Task GenerateIndustryAnalysisAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        context.IndustryInsights.Add(new IndustryInsight
        {
            ClientCompanyId = clientId,
            Topic = $"{client.Industry ?? "Industry"} AI adoption themes",
            InsightText = $"Organizations in {client.Industry ?? "this industry"} are prioritizing internal productivity assistants, document intelligence, decision support, and carefully governed customer-facing automation.",
            Relevance = "Use as a starting draft and replace with sourced research during consultant review.",
            SourceType = InsightSourceType.AiGenerated,
            Status = InsightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        await AddAnalysisOutputAsync(client, AnalysisType.IndustryAnalysis, "Industry analysis draft", "Placeholder industry analysis generated. Add sourced consultant research before approving.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Industry analysis generated", "Placeholder industry insight saved.");
    }

    public async Task GenerateCompetitorInsightsAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        context.CompetitorInsights.Add(new CompetitorInsight
        {
            ClientCompanyId = clientId,
            CompetitorName = "Representative competitor",
            InsightText = $"A comparable {client.Industry ?? "industry"} organization may use AI for customer support, knowledge retrieval, workflow automation, and analytics acceleration.",
            AiUseCasesObserved = "Support assistant; internal knowledge assistant; automated reporting.",
            StrengthComparedToClient = "May move faster if governance and data foundations are already clear.",
            WeaknessComparedToClient = "May lack the client's domain-specific process knowledge.",
            SourceNotes = "Placeholder only; replace with web research or consultant-verified evidence.",
            Status = InsightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        await AddAnalysisOutputAsync(client, AnalysisType.CompetitorInsights, "Competitor insight draft", "Placeholder competitor insight generated. Validate sources before approval.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Competitor insights generated", "Placeholder competitor insight saved.");
    }

    public async Task GenerateUseCasesAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
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

        foreach (var useCase in useCases)
        {
            var exists = await context.AIUseCases.AnyAsync(existing => existing.ClientCompanyId == clientId && existing.Title == useCase.Item1);
            if (exists)
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
        }

        await AddAnalysisOutputAsync(client, AnalysisType.UseCaseGeneration, "AI use case suggestions", "Suggested use cases generated from the default consulting library. Shortlist the top three after scoring.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Use cases generated", "Default AI use case library suggestions saved.");
    }

    public async Task ScoreUseCasesAsync(int clientId)
    {
        var useCases = await context.AIUseCases
            .Include(useCase => useCase.Score)
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .ToListAsync();

        foreach (var useCase in useCases)
        {
            var score = useCase.Score ?? new AIUseCaseScore { AIUseCaseId = useCase.Id };
            score.RoiScore = useCase.Title.Contains("report", StringComparison.OrdinalIgnoreCase) ? 5 : 4;
            score.FeasibilityScore = useCase.ImplementationComplexity == ComplexityLevel.Low ? 5 : useCase.ImplementationComplexity == ComplexityLevel.Medium ? 4 : 3;
            score.RiskSafetyScore = useCase.RiskLevel == RiskLevel.Low ? 5 : useCase.RiskLevel == RiskLevel.Medium ? 3 : 2;
            score.StrategicFitScore = 4;
            score.DataReadinessScore = useCase.RequiredData?.Contains("approved", StringComparison.OrdinalIgnoreCase) == true ? 4 : 3;
            score.ScoringComment = "Mock weighted score using ROI, feasibility, strategic fit, data readiness, and risk/safety.";
            score.LastModifiedAt = DateTime.UtcNow;
            score.RecalculatePriority();

            if (useCase.Score is null)
            {
                useCase.Score = score;
            }
        }

        var client = await LoadClientAsync(clientId);
        await AddAnalysisOutputAsync(client, AnalysisType.UseCaseScoring, "Use case scoring draft", "Use cases scored with the MVP weighted rule. Consultant should adjust scores where client context warrants.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Use cases scored", "Use case priority scores recalculated.");
    }

    public async Task GenerateReadinessScoreAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        var answerCount = GetLatestEvidenceResponse(client)?.Answers.Count(answer => !string.IsNullOrWhiteSpace(answer.AnswerText)) ?? 0;
        var openCriticalOrHighGaps = client.GapAnalysisItems.Count(gap => gap.Status == GapStatus.Open && gap.Severity is Severity.High or Severity.Critical);

        var business = Clamp(45 + answerCount * 5);
        var data = Clamp(55 - openCriticalOrHighGaps * 6 + CountEvidence(client, "data") * 4);
        var process = Clamp(50 + CountEvidence(client, "process") * 8);
        var technology = Clamp(55 + CountEvidence(client, "tool") * 5);
        var governance = Clamp(45 + CountEvidence(client, "governance") * 10 - openCriticalOrHighGaps * 5);
        var overall = (int)Math.Round(0.20 * business + 0.25 * data + 0.20 * process + 0.15 * technology + 0.20 * governance);

        var score = new ReadinessScore
        {
            ClientCompanyId = clientId,
            BusinessClarityScore = business,
            DataReadinessScore = data,
            ProcessReadinessScore = process,
            TechnologyReadinessScore = technology,
            PeopleGovernanceScore = governance,
            OverallScore = overall,
            ScoreCategory = GetScoreCategory(overall),
            ScoringSummary = "MVP score generated from assessment completeness, evidence signals, and unresolved high-severity gaps.",
            CreatedAt = DateTime.UtcNow
        };

        context.ReadinessScores.Add(score);
        client.OverallReadinessScore = overall;
        client.NextAction = "Review readiness score rationale";
        Touch(client);
        await AddAnalysisOutputAsync(client, AnalysisType.RiskAnalysis, "Readiness score rationale", score.ScoringSummary);
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Readiness score generated", $"Overall score generated: {overall} ({score.ScoreCategory}).");
    }

    public async Task GenerateRoadmapAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        var useCases = await context.AIUseCases
            .Include(useCase => useCase.Score)
            .Where(useCase => useCase.ClientCompanyId == clientId)
            .OrderByDescending(useCase => useCase.Score == null ? 0 : useCase.Score.PriorityScore)
            .Take(4)
            .ToListAsync();

        if (!useCases.Any())
        {
            await GenerateUseCasesAsync(clientId);
            useCases = await context.AIUseCases
                .Include(useCase => useCase.Score)
                .Where(useCase => useCase.ClientCompanyId == clientId)
                .Take(4)
                .ToListAsync();
        }

        var phases = new[] { RoadmapPhase.ZeroToThreeMonths, RoadmapPhase.ThreeToSixMonths, RoadmapPhase.SixToTwelveMonths, RoadmapPhase.TwelvePlusMonths };
        for (var i = 0; i < useCases.Count; i++)
        {
            var useCase = useCases[i];
            context.AIRoadmapItems.Add(new AIRoadmapItem
            {
                ClientCompanyId = clientId,
                Phase = phases[Math.Min(i, phases.Length - 1)],
                Title = useCase.Title,
                Description = $"Plan and deliver a controlled pilot for {useCase.Title}.",
                RelatedUseCaseId = useCase.Id,
                Owner = useCase.Department,
                ExpectedOutcome = useCase.ExpectedBenefit,
                Dependencies = "Approved data access, use-case owner, success metric, and risk review.",
                Status = ApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow
            });
        }

        await AddAnalysisOutputAsync(client, AnalysisType.Roadmap, "Roadmap draft", "Roadmap generated from top use cases and known readiness gaps.");
        await context.SaveChangesAsync();
        await LogAsync(clientId, "Roadmap generated", "Roadmap draft generated from use case priorities.");
    }

    public async Task GenerateReportDraftAsync(int clientId)
    {
        var client = await LoadClientAsync(clientId);
        var version = await context.ClientReports
            .Where(report => report.ClientCompanyId == clientId)
            .Select(report => (int?)report.VersionNumber)
            .MaxAsync() ?? 0;

        var latestScore = client.ReadinessScores.OrderByDescending(score => score.CreatedAt).FirstOrDefault();
        var report = new ClientReport
        {
            ClientCompanyId = clientId,
            ReportTitle = $"{client.CompanyName} AI Readiness Report",
            ReportStatus = ReportStatus.DraftGenerated,
            VersionNumber = version + 1,
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            FinalReportContent = $"Draft report for {client.CompanyName}. Overall score: {latestScore?.OverallScore.ToString() ?? "not generated"}."
        };

        for (var i = 0; i < ReportSections.Length; i++)
        {
            report.Sections.Add(new ReportSection
            {
                SectionTitle = ReportSections[i],
                SectionOrder = i + 1,
                SectionContent = BuildReportSectionDraft(client, ReportSections[i], latestScore),
                SectionStatus = SectionStatus.DraftGenerated,
                CreatedAt = DateTime.UtcNow
            });
        }

        context.ClientReports.Add(report);
        client.CurrentStage = ClientStage.ReportDraft;
        client.NextAction = "Review and approve report sections";
        Touch(client);
        await AddAnalysisOutputAsync(client, AnalysisType.FinalReportSection, "Report draft generated", "A full report draft with editable sections has been generated.");
        await context.SaveChangesAsync();
        await MarkWorkflowAsync(clientId, "Report Draft Generated", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Report draft generated", $"Report version {report.VersionNumber} generated.");
    }

    private async Task<ClientCompany> LoadClientAsync(int clientId)
    {
        return await context.ClientCompanies
            .Include(client => client.WorkflowSteps)
            .Include(client => client.ReadinessAssessments)
                .ThenInclude(assessment => assessment.Answers)
            .Include(client => client.ReadinessAssessments)
                .ThenInclude(assessment => assessment.Responses)
                    .ThenInclude(response => response.Answers)
            .Include(client => client.Documents)
            .Include(client => client.Notes)
            .Include(client => client.MeetingTranscripts)
            .Include(client => client.GapAnalysisItems)
            .Include(client => client.SwotItems)
            .Include(client => client.IndustryInsights)
            .Include(client => client.CompetitorInsights)
            .Include(client => client.UseCases)
                .ThenInclude(useCase => useCase.Score)
            .Include(client => client.ReadinessScores)
            .Include(client => client.RoadmapItems)
            .Include(client => client.Reports)
                .ThenInclude(report => report.Sections)
            .FirstAsync(client => client.Id == clientId);
    }

    private async Task AddAnalysisOutputAsync(ClientCompany client, AnalysisType type, string title, string content)
    {
        var version = await context.AIAnalysisOutputs
            .Where(output => output.ClientCompanyId == client.Id && output.AnalysisType == type)
            .Select(output => (int?)output.VersionNumber)
            .MaxAsync() ?? 0;

        context.AIAnalysisOutputs.Add(new AIAnalysisOutput
        {
            ClientCompanyId = client.Id,
            AnalysisType = type,
            Title = title,
            InputSummary = BuildInputSummary(client),
            OutputContent = content,
            Status = DraftStatus.DraftGenerated,
            VersionNumber = version + 1,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "MockAI",
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildInputSummary(ClientCompany client)
    {
        var latestResponse = GetLatestEvidenceResponse(client);
        var assessmentCount = latestResponse?.Answers.Count ?? 0;
        var responseLabel = latestResponse?.ResponseLabel ?? "no selected response";
        return $"{client.CompanyName}; {client.Industry ?? "industry not set"}; {assessmentCount} assessment answers from {responseLabel}; {client.Documents.Count} documents; {client.Notes.Count} notes; {client.GapAnalysisItems.Count} gaps.";
    }

    private static string BuildEvidenceText(ClientCompany client)
    {
        // MVP default: generated analyses use the latest non-ignored assessment response
        // that has answers, so repeat submissions are not merged into one evidence set.
        var latestResponse = GetLatestEvidenceResponse(client);
        var values = new List<string?>
        {
            client.CompanyName,
            client.Industry,
            client.BusinessModel,
            client.KeyRisksSummary,
            client.NextAction
        };
        values.AddRange(latestResponse?.Answers.Select(answer => $"{answer.QuestionText} {answer.AnswerText} {answer.SectionName}") ?? []);
        values.AddRange(client.Documents.Select(document => $"{document.Description} {document.AiSummary} {document.KeyInsights}"));
        values.AddRange(client.Notes.Select(note => $"{note.NoteTitle} {note.NoteText}"));
        values.AddRange(client.MeetingTranscripts.Select(transcript => $"{transcript.TranscriptText} {transcript.Summary} {transcript.KeyDecisions} {transcript.FollowUpQuestions}"));
        return string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
    }

    private static AssessmentResponse? GetLatestEvidenceResponse(ClientCompany client)
    {
        return client.ReadinessAssessments
            .SelectMany(assessment => assessment.Responses)
            .Where(response => response.Status != AssessmentResponseStatus.Ignored && response.Answers.Any())
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .FirstOrDefault();
    }

    private static int CountEvidence(ClientCompany client, string keyword)
    {
        return BuildEvidenceText(client).Split(keyword, StringSplitOptions.RemoveEmptyEntries).Length - 1;
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

    private static string BuildGapOutput(ClientCompany client, IReadOnlyCollection<GapAnalysisItem> gaps)
    {
        if (gaps.Count == 0)
        {
            return $"No new rule-based gaps were detected for {client.CompanyName}. Consultant should still review completeness manually.";
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
            <= 30 => ScoreCategory.AiBeginner,
            <= 60 => ScoreCategory.ExplorationReady,
            <= 80 => ScoreCategory.PilotReady,
            _ => ScoreCategory.ImplementationReady
        };
    }

    private static string BuildReportSectionDraft(ClientCompany client, string sectionTitle, ReadinessScore? latestScore)
    {
        return sectionTitle switch
        {
            "Executive Summary" => $"{client.CompanyName} shows {latestScore?.ScoreCategory.ToString() ?? "an unscored readiness profile"} with practical opportunities that should be validated by the consultant.",
            "AI Readiness Score" => latestScore is null ? "Readiness score has not been generated yet." : $"Overall score: {latestScore.OverallScore}/100. {latestScore.ScoringSummary}",
            "Gap Analysis" => client.GapAnalysisItems.Any() ? string.Join(Environment.NewLine, client.GapAnalysisItems.Take(5).Select(gap => $"- {gap.GapArea}: {gap.IssueDescription}")) : "No gap analysis items available yet.",
            "SWOT Analysis" => client.SwotItems.Any() ? string.Join(Environment.NewLine, client.SwotItems.Take(8).Select(item => $"- {item.Category}: {item.Description}")) : "No SWOT draft available yet.",
            "Recommended AI Use Cases" => client.UseCases.Any() ? string.Join(Environment.NewLine, client.UseCases.Take(5).Select(useCase => $"- {useCase.Title}: {useCase.Description}")) : "No AI use cases have been generated yet.",
            "Use Case Scoring" => client.UseCases.Any(useCase => useCase.Score is not null) ? string.Join(Environment.NewLine, client.UseCases.Where(useCase => useCase.Score is not null).Take(5).Select(useCase => $"- {useCase.Title}: {useCase.Score!.PriorityScore}/5")) : "Use cases are not scored yet.",
            "1-Year Roadmap" => client.RoadmapItems.Any() ? string.Join(Environment.NewLine, client.RoadmapItems.Take(5).Select(item => $"- {item.Phase}: {item.Title}")) : "Roadmap has not been generated yet.",
            _ => $"Draft {sectionTitle.ToLowerInvariant()} content for {client.CompanyName}. Consultant review required before approval."
        };
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var step = await context.ClientWorkflowSteps.FirstOrDefaultAsync(item => item.ClientCompanyId == clientId && item.StageName == stageName);
        if (step is null)
        {
            return;
        }

        step.Status = status;
        step.CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : step.CompletedAt;
        await context.SaveChangesAsync();
    }

    private async Task LogAsync(int clientId, string activityType, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = activityType,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        });
        await context.SaveChangesAsync();
    }

    private static void Touch(ClientCompany client)
    {
        client.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedBy = "System";
    }
}
