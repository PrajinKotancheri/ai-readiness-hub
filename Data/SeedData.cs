using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Data;

public static class SeedData
{
    private static readonly DateTime SeedNow = new(2026, 6, 3, 9, 0, 0, DateTimeKind.Utc);

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await SeedPromptDefinitionsAsync(context);
        await SeedReportTemplateSectionsAsync(context);
        await SeedUseCaseLibraryAsync(context);
        await context.SaveChangesAsync();

        if (await context.ClientCompanies.AnyAsync())
        {
            return;
        }

        var greenTech = new ClientCompany
        {
            CompanyName = "GreenTech Solutions AG",
            Industry = "Renewable Energy",
            WebsiteUrl = "https://greentech.example",
            Country = "Switzerland",
            Region = "Zurich",
            CompanySizeRange = "51-200",
            RevenueRange = "CHF 10M-50M",
            BusinessModel = "B2B energy software and services",
            ContactPersonName = "Mara Keller",
            ContactPersonEmail = "mara.keller@greentech.example",
            ConsultingPackage = "AI Readiness Sprint",
            AssignedConsultant = "Prajin",
            Priority = TaskPriority.High,
            CurrentStage = ClientStage.AssessmentCompleted,
            OverallReadinessScore = 62,
            KeyRisksSummary = "Data ownership and governance responsibilities need clarification.",
            NextAction = "Generate knowledge gap analysis",
            CreatedAt = SeedNow.AddDays(-21),
            CreatedBy = "Seed"
        };

        var alpine = new ClientCompany
        {
            CompanyName = "Alpine Retail GmbH",
            Industry = "Retail",
            WebsiteUrl = "https://alpine-retail.example",
            Country = "Germany",
            Region = "Bavaria",
            CompanySizeRange = "201-500",
            RevenueRange = "EUR 50M-100M",
            BusinessModel = "Omnichannel retail",
            ContactPersonName = "Jonas Weiss",
            ContactPersonEmail = "jonas.weiss@alpine-retail.example",
            ConsultingPackage = "Readiness + Roadmap",
            AssignedConsultant = "Prajin",
            Priority = TaskPriority.Medium,
            CurrentStage = ClientStage.KnowledgeGapAnalysis,
            OverallReadinessScore = 55,
            KeyRisksSummary = "Manual processes are clear, but data quality and policy maturity are uneven.",
            NextAction = "Review generated SWOT",
            CreatedAt = SeedNow.AddDays(-16),
            CreatedBy = "Seed"
        };

        var eduFuture = new ClientCompany
        {
            CompanyName = "EduFuture Learning Ltd",
            Industry = "Education",
            WebsiteUrl = "https://edufuture.example",
            Country = "United Kingdom",
            Region = "London",
            CompanySizeRange = "51-200",
            RevenueRange = "GBP 5M-10M",
            BusinessModel = "B2B/B2C learning platform",
            ContactPersonName = "Leah Morris",
            ContactPersonEmail = "leah.morris@edufuture.example",
            ConsultingPackage = "Executive Report",
            AssignedConsultant = "Aisha",
            Priority = TaskPriority.High,
            CurrentStage = ClientStage.ReportDraft,
            OverallReadinessScore = 74,
            KeyRisksSummary = "Responsible AI controls must be formalized before learner-facing automation.",
            NextAction = "Consultant review of report draft",
            CreatedAt = SeedNow.AddDays(-28),
            CreatedBy = "Seed"
        };

        context.ClientCompanies.AddRange(greenTech, alpine, eduFuture);
        await context.SaveChangesAsync();

        AddWorkflow(greenTech, 3);
        AddWorkflow(alpine, 5);
        AddWorkflow(eduFuture, 15);

        var greenAssessment = new ReadinessAssessment
        {
            ClientCompanyId = greenTech.Id,
            FormStatus = ReadinessFormStatus.Completed,
            FormUrl = "https://forms.example/greentech",
            SentAt = SeedNow.AddDays(-18),
            CompletedAt = SeedNow.AddDays(-13),
            ImportedAt = SeedNow.AddDays(-12),
            Summary = "Leadership has clear growth goals and several candidate AI use cases. Data ownership is still informal.",
            RawResponseJson = "{ \"business_goal\": \"Reduce reporting time and improve asset maintenance planning\" }",
            CreatedAt = SeedNow.AddDays(-18)
        };
        var greenResponse = new AssessmentResponse
        {
            ResponseNumber = 1,
            ResponseLabel = "First response",
            Source = AssessmentResponseSource.ExistingImport,
            Status = AssessmentResponseStatus.Imported,
            SubmittedAt = SeedNow.AddDays(-13),
            ReceivedAt = SeedNow.AddDays(-12),
            AnswerCount = 3,
            RawResponseJson = greenAssessment.RawResponseJson,
            CreatedAt = SeedNow.AddDays(-12)
        };
        greenAssessment.Responses.Add(greenResponse);

        AddSeedAnswer(greenAssessment, greenResponse, new AssessmentAnswer
        {
            SectionName = "Business Goals",
            QuestionText = "What are the primary business goals for AI?",
            AnswerText = "Reduce internal reporting time and improve predictive maintenance planning.",
            AnswerType = "Long text",
            IsMandatory = true,
            CompletenessStatus = CompletenessStatus.Complete,
            CreatedAt = SeedNow.AddDays(-12)
        });
        AddSeedAnswer(greenAssessment, greenResponse, new AssessmentAnswer
        {
            SectionName = "Data Readiness",
            QuestionText = "Which data sources are available?",
            AnswerText = "Asset performance logs, maintenance records, CRM notes, and project delivery reports.",
            AnswerType = "Long text",
            IsMandatory = true,
            CompletenessStatus = CompletenessStatus.Partial,
            CreatedAt = SeedNow.AddDays(-12)
        });
        AddSeedAnswer(greenAssessment, greenResponse, new AssessmentAnswer
        {
            SectionName = "Governance",
            QuestionText = "Is there an AI governance policy?",
            AnswerText = "",
            AnswerType = "Long text",
            IsMandatory = true,
            CompletenessStatus = CompletenessStatus.Missing,
            CreatedAt = SeedNow.AddDays(-12)
        });
        context.ReadinessAssessments.Add(greenAssessment);

        context.GapAnalysisItems.AddRange(
            new GapAnalysisItem
            {
                ClientCompanyId = greenTech.Id,
                GapArea = GapArea.Governance,
                IssueDescription = "No formal AI governance policy has been identified.",
                Impact = "Consultants cannot yet validate risk controls for client-facing or operational AI.",
                Severity = Severity.High,
                SuggestedFollowUpQuestion = "Who owns AI policy, model review, and usage approval?",
                SuggestedAction = "Create an AI governance owner and lightweight policy before pilots.",
                CreatedAt = SeedNow.AddDays(-11)
            },
            new GapAnalysisItem
            {
                ClientCompanyId = greenTech.Id,
                GapArea = GapArea.Ownership,
                IssueDescription = "AI sponsorship and process owners are not explicit.",
                Impact = "Pilot momentum may slow without accountable owners.",
                Severity = Severity.Medium,
                SuggestedFollowUpQuestion = "Which leader will own the first AI pilot portfolio?",
                SuggestedAction = "Assign an executive sponsor and use-case owners.",
                CreatedAt = SeedNow.AddDays(-11)
            });

        context.KnowledgeGapItems.Add(new KnowledgeGapItem
        {
            ClientCompanyId = greenTech.Id,
            AssessmentResponse = greenResponse,
            GapArea = KnowledgeGapArea.GovernanceCompliance,
            MissingInformation = "AI governance ownership and approval responsibilities are unclear.",
            WhyItMatters = "The consultant cannot safely recommend client-facing or regulated pilots without understanding who approves AI use.",
            FollowUpQuestion = "Who owns AI policy, model review, data privacy approval, and usage monitoring?",
            SuggestedEvidence = "AI policy draft, risk register, approval workflow, or named governance owner.",
            Priority = KnowledgeGapPriority.High,
            Status = KnowledgeGapStatus.Open,
            CreatedAt = SeedNow.AddDays(-11)
        });
        context.AIOutputSources.Add(new AIOutputSource
        {
            ClientCompanyId = greenTech.Id,
            OutputType = AIOutputType.KnowledgeGap,
            SourceType = AIOutputSourceType.Internal,
            SourceCategory = AIOutputSourceCategory.AssessmentResponse,
            SourceLabel = "Assessment Response: First response",
            SourceReference = "Governance answer",
            EvidenceText = "Governance answer was missing in the imported assessment response.",
            CreatedAt = SeedNow.AddDays(-11)
        });

        AddUseCase(context, greenTech.Id, "Automated report generation", "Generate first-draft operational reports from asset and maintenance data.", "Operations", 4.15m);
        AddUseCase(context, greenTech.Id, "Internal knowledge assistant", "Answer consultant and engineer questions from policies, project history, and maintenance guidance.", "Operations", 3.75m);

        context.ClientDocuments.AddRange(
            new ClientDocument
            {
                ClientCompanyId = alpine.Id,
                FileName = "Store operations process map.pdf",
                FilePath = "/uploads/alpine/process-map.pdf",
                DocumentType = DocumentType.ProcessDocument,
                Description = "Current store replenishment and customer service process map.",
                UploadedBy = "Prajin",
                AiSummary = "Highlights manual handoffs between store teams, inventory planners, and support.",
                KeyInsights = "High opportunity in support automation and stock exception triage.",
                UsedInReport = true,
                UploadedAt = SeedNow.AddDays(-9),
                CreatedAt = SeedNow.AddDays(-9)
            },
            new ClientDocument
            {
                ClientCompanyId = alpine.Id,
                FileName = "Customer service transcripts sample.txt",
                FilePath = "/uploads/alpine/support-sample.txt",
                DocumentType = DocumentType.MeetingTranscript,
                Description = "Sample anonymized support conversations.",
                UploadedBy = "Prajin",
                AiSummary = "Repeated questions around returns, delivery delays, and loyalty points.",
                KeyInsights = "Suitable for a bounded customer support assistant after policy review.",
                UsedInReport = false,
                UploadedAt = SeedNow.AddDays(-7),
                CreatedAt = SeedNow.AddDays(-7)
            });

        context.ConsultantNotes.Add(new ConsultantNote
        {
            ClientCompanyId = alpine.Id,
            NoteTitle = "Discovery call notes",
            NoteText = "Retail team wants quick wins in service operations, but legal wants approval before customer-facing AI.",
            NoteType = NoteType.MeetingNote,
            CreatedBy = "Prajin",
            CreatedAt = SeedNow.AddDays(-8)
        });

        context.SwotAnalysisItems.AddRange(
            new SwotAnalysisItem
            {
                ClientCompanyId = alpine.Id,
                Category = SwotCategory.Strength,
                Description = "Clear operational pain points and measurable service volume.",
                EvidenceSource = "Discovery note and process document",
                Status = ItemReviewStatus.Draft,
                CreatedAt = SeedNow.AddDays(-6)
            },
            new SwotAnalysisItem
            {
                ClientCompanyId = alpine.Id,
                Category = SwotCategory.Weakness,
                Description = "Customer-facing AI policy is not yet approved.",
                EvidenceSource = "Consultant note",
                Status = ItemReviewStatus.Draft,
                CreatedAt = SeedNow.AddDays(-6)
            },
            new SwotAnalysisItem
            {
                ClientCompanyId = alpine.Id,
                Category = SwotCategory.Opportunity,
                Description = "Support assistant could reduce repetitive ticket load.",
                EvidenceSource = "Support transcripts",
                Status = ItemReviewStatus.Draft,
                CreatedAt = SeedNow.AddDays(-6)
            },
            new SwotAnalysisItem
            {
                ClientCompanyId = alpine.Id,
                Category = SwotCategory.Threat,
                Description = "Poorly governed customer-facing automation could damage trust.",
                EvidenceSource = "Consultant assessment",
                Status = ItemReviewStatus.Draft,
                CreatedAt = SeedNow.AddDays(-6)
            });

        AddUseCase(context, eduFuture.Id, "Learning content assistant", "Create draft lesson activities and quiz variations from approved curriculum assets.", "Product", 4.25m);
        AddUseCase(context, eduFuture.Id, "Meeting summary and action tracker", "Summarize customer success calls and route follow-up actions.", "Customer Success", 3.95m);

        var eduScore = new ReadinessScore
        {
            ClientCompanyId = eduFuture.Id,
            BusinessClarityScore = 78,
            DataReadinessScore = 70,
            ProcessReadinessScore = 76,
            TechnologyReadinessScore = 72,
            PeopleGovernanceScore = 70,
            OverallScore = 74,
            ScoreCategory = ScoreCategory.PilotReady,
            ScoringSummary = "Strong pilot potential with a need for stricter learner-facing guardrails.",
            CreatedAt = SeedNow.AddDays(-5)
        };
        context.ReadinessScores.Add(eduScore);

        var eduReport = new ClientReport
        {
            ClientCompanyId = eduFuture.Id,
            ReportTitle = "EduFuture Learning Ltd AI Readiness Report",
            ReportStatus = ReportStatus.DraftGenerated,
            VersionNumber = 1,
            GeneratedAt = SeedNow.AddDays(-3),
            CreatedAt = SeedNow.AddDays(-3),
            FinalReportContent = "Draft report content assembled from approved sections."
        };
        AddDefaultReportSections(eduReport, "Draft section prepared for consultant review.");
        context.ClientReports.Add(eduReport);

        context.ClientTasks.AddRange(
            new ClientTask
            {
                ClientCompanyId = greenTech.Id,
                TaskTitle = "Clarify AI governance owner",
                TaskDescription = "Ask client to nominate policy owner and pilot approval route.",
                TaskType = TaskType.FollowUp,
                AssignedTo = "Prajin",
                DueDate = SeedNow.AddDays(2),
                Priority = TaskPriority.High,
                Status = ClientTaskStatus.Open,
                CreatedAt = SeedNow.AddDays(-2)
            },
            new ClientTask
            {
                ClientCompanyId = alpine.Id,
                TaskTitle = "Review generated SWOT",
                TaskDescription = "Validate each item before it can feed the report.",
                TaskType = TaskType.ReviewAiOutput,
                AssignedTo = "Prajin",
                DueDate = SeedNow.AddDays(1),
                Priority = TaskPriority.Medium,
                Status = ClientTaskStatus.InProgress,
                CreatedAt = SeedNow.AddDays(-3)
            },
            new ClientTask
            {
                ClientCompanyId = eduFuture.Id,
                TaskTitle = "Approve report sections",
                TaskDescription = "Executive summary and risks sections need consultant review.",
                TaskType = TaskType.FinalizeReport,
                AssignedTo = "Aisha",
                DueDate = SeedNow.AddDays(4),
                Priority = TaskPriority.High,
                Status = ClientTaskStatus.Open,
                CreatedAt = SeedNow.AddDays(-2)
            });

        await context.SaveChangesAsync();

        AddActivityLogs(context, greenTech.Id, "Client created", "Seed client and assessment imported.");
        AddActivityLogs(context, alpine.Id, "SWOT generated", "Seed SWOT draft generated from notes and documents.");
        AddActivityLogs(context, eduFuture.Id, "Report draft generated", "Seed report draft created for consultant review.");
        await context.SaveChangesAsync();
    }

    private static void AddWorkflow(ClientCompany client, int completedThrough)
    {
        foreach (var (stage, index) in StakeholderWorkflow.Stages.Select((stage, index) => (stage, index)))
        {
            var status = index + 1 < completedThrough
                ? WorkflowStepStatus.Completed
                : index + 1 == completedThrough
                    ? WorkflowStepStatus.InProgress
                    : WorkflowStepStatus.NotStarted;

            client.WorkflowSteps.Add(new ClientWorkflowStep
            {
                StageName = stage,
                DisplayOrder = index + 1,
                Status = status,
                CompletedAt = status == WorkflowStepStatus.Completed ? SeedNow.AddDays(-20 + index) : null
            });
        }
    }

    private static void AddUseCase(ApplicationDbContext context, int clientId, string title, string description, string department, decimal priorityScore)
    {
        var useCase = new AIUseCase
        {
            ClientCompanyId = clientId,
            Title = title,
            Description = description,
            BusinessProblem = "Reduce repeated manual work and improve decision speed.",
            Department = department,
            ExpectedBenefit = "Faster delivery, better quality, and reusable knowledge capture.",
            RequiredData = "Approved internal documents, process data, and relevant operational records.",
            ImplementationComplexity = priorityScore >= 4 ? ComplexityLevel.Medium : ComplexityLevel.Low,
            RiskLevel = RiskLevel.Medium,
            TimeToValue = TimeToValue.ThreeToSixMonths,
            Status = priorityScore >= 4 ? UseCaseStatus.Shortlisted : UseCaseStatus.Suggested,
            CreatedAt = SeedNow.AddDays(-5)
        };
        useCase.Score = new AIUseCaseScore
        {
            RoiScore = priorityScore >= 4 ? 5 : 4,
            FeasibilityScore = priorityScore >= 4 ? 4 : 3,
            RiskSafetyScore = 3,
            StrategicFitScore = 4,
            DataReadinessScore = 4,
            PriorityScore = priorityScore,
            ScoringComment = "Seed score based on initial consultant judgement.",
            CreatedAt = SeedNow.AddDays(-5)
        };
        context.AIUseCases.Add(useCase);
    }

    private static void AddSeedAnswer(ReadinessAssessment assessment, AssessmentResponse response, AssessmentAnswer answer)
    {
        answer.ReadinessAssessment = assessment;
        answer.AssessmentResponse = response;
        assessment.Answers.Add(answer);
        response.Answers.Add(answer);
    }

    private static void AddDefaultReportSections(ClientReport report, string content)
    {
        var sections = new[]
        {
            "Cover / Client Details",
            "Personal Note",
            "AI Readiness Summary",
            "Strengths & Development Areas",
            "AI Readiness Deep-Dive",
            "Competitive Snapshot",
            "Top Recommended AI Use Cases",
            "Recommended Roadmap",
            "Next Steps / How We Can Help"
        };

        for (var index = 0; index < sections.Length; index++)
        {
            report.Sections.Add(new ReportSection
            {
                SectionTitle = sections[index],
                SectionOrder = index + 1,
                SectionContent = $"{content} ({sections[index]})",
                SectionStatus = SectionStatus.DraftGenerated,
                CreatedAt = SeedNow.AddDays(-3)
            });
        }
    }

    private static void AddActivityLogs(ApplicationDbContext context, int clientId, string type, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = type,
            Description = description,
            CreatedBy = "Seed",
            CreatedAt = SeedNow
        });
    }

    private static async Task SeedPromptDefinitionsAsync(ApplicationDbContext context)
    {
        var prompts = new (string Name, string Goal, string Inputs, string Outputs, string Location, string Notes)[]
        {
            ("Company Summary", "Summarize the client context for consultant review.", "Client profile, assessment response, notes, documents", "Editable company summary draft", "Workspace > Company Summary", "Feeds later analysis after approval."),
            ("Knowledge Gap Analysis", "Identify missing consultant understanding before conclusions.", "Client profile, latest assessment, missing answers, notes/documents", "Knowledge gap items and discovery agenda", "Workspace > Knowledge Gap Analysis", "First AI activity after assessment."),
            ("Readiness Score", "Calculate readiness dimensions aligned to the strategic report.", "Assessment answers, approved evidence", "Readiness score, adoption profile, benchmark", "Workspace > Assessment Responses", "Uses stakeholder score labels."),
            ("Industry Analysis", "Create an industry context draft with sources.", "Approved company summary, documents, external research", "Editable industry analysis", "Workspace > Industry & Competitors", "External sources to be added later."),
            ("Competitor Analysis", "Compare representative competitors and AI use signals.", "Approved industry analysis, competitor URLs, research", "Editable competitor insight draft", "Workspace > Industry & Competitors", "Requires sourced validation."),
            ("SWOT Analysis", "Synthesize strengths, weaknesses, opportunities, and threats.", "Approved company/industry/competitor outputs", "Editable SWOT draft", "Workspace > SWOT", "Feeds use case identification."),
            ("Use Case Identification", "Identify suitable AI use cases from approved evidence.", "Approved SWOT, knowledge gaps, client goals", "Use case shortlist", "Workspace > Use Cases & Scoring", "Future: curated library plus research."),
            ("Use Case Scoring", "Prioritize use cases by ROI, feasibility, fit, data readiness, and risk.", "Use case shortlist, readiness score", "Weighted use case scores", "Workspace > Use Cases & Scoring", "Consultant can override scores."),
            ("Roadmap Generation", "Create a phased roadmap from approved use cases.", "Approved use cases and scores", "Roadmap phases and dependencies", "Workspace > Roadmap", "Feeds strategic report."),
            ("Strategic Report Generation", "Assemble the consultant-reviewed strategic report.", "Approved outputs and report template", "Editable report sections", "Workspace > Strategic Report", "Final report is not automated without approval.")
        };

        var existing = await context.PromptDefinitions
            .Select(prompt => prompt.PromptName)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var prompt in prompts)
        {
            if (existingSet.Contains(prompt.Name))
            {
                continue;
            }

            context.PromptDefinitions.Add(new PromptDefinition
            {
                PromptName = prompt.Name,
                Goal = prompt.Goal,
                Inputs = prompt.Inputs,
                Outputs = prompt.Outputs,
                PlatformLocation = prompt.Location,
                PromptText = "Stakeholder to provide actual prompt.",
                Notes = prompt.Notes,
                Status = PromptStatus.Draft,
                VersionNumber = 1,
                CreatedAt = SeedNow
            });
        }
    }

    private static async Task SeedReportTemplateSectionsAsync(ApplicationDbContext context)
    {
        var sections = new (string Title, string Goal)[]
        {
            ("Cover / Client Details", "Identify company, industry, assessment date, and prepared-by details."),
            ("Personal Note", "Provide consultant-authored context and relationship framing."),
            ("AI Readiness Summary", "Summarize readiness score, adoption profile, benchmark, and interpretation."),
            ("Strengths & Development Areas", "Show dimensions, scores, status, and interpretation."),
            ("AI Readiness Deep-Dive", "Explain strategic context, governance/compliance, data readiness, and operational bottlenecks."),
            ("Competitive Snapshot", "Summarize industry trends, competitor examples, and key takeaway."),
            ("Top Recommended AI Use Cases", "List the strongest recommended AI use cases and expected outcomes."),
            ("Recommended Roadmap", "Lay out foundation, pilot/prove, and scale/position phases."),
            ("Next Steps / How We Can Help", "Explain advisory, prioritization, governance, delivery, and partner support.")
        };

        var existing = await context.ReportTemplateSections
            .Select(section => section.SectionTitle)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < sections.Length; index++)
        {
            if (existingSet.Contains(sections[index].Title))
            {
                continue;
            }

            context.ReportTemplateSections.Add(new ReportTemplateSection
            {
                SectionTitle = sections[index].Title,
                SectionOrder = index + 1,
                SectionGoal = sections[index].Goal,
                DefaultPrompt = "Stakeholder to provide actual prompt.",
                Status = ReportTemplateSectionStatus.Active,
                CreatedAt = SeedNow
            });
        }
    }

    private static async Task SeedUseCaseLibraryAsync(ApplicationDbContext context)
    {
        var existing = await context.UseCaseLibraryItems
            .Select(item => item.Name)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var items = new[]
        {
            ("Internal knowledge assistant", "Answers employee questions from approved internal documents.", "Operations, Professional Services, SaaS"),
            ("Automated report generation", "Creates first-draft management or operational reports.", "Operations, Consulting, Energy, Finance"),
            ("Meeting summary and action tracker", "Summarizes meetings and creates follow-up tasks.", "Sales, Customer Success, Consulting")
        };

        foreach (var item in items)
        {
            if (existingSet.Contains(item.Item1))
            {
                continue;
            }

            context.UseCaseLibraryItems.Add(new UseCaseLibraryItem
            {
                Name = item.Item1,
                Description = item.Item2,
                ApplicableIndustries = item.Item3,
                SuccessCriteria = "Time saved, quality reviewed, and adoption measured.",
                TypicalRoi = "Medium to high depending on process volume.",
                Evidence = "Placeholder library entry. Stakeholder curation required.",
                Complexity = ComplexityLevel.Medium,
                Dependencies = "Approved data access, responsible owner, review workflow.",
                CreatedAt = SeedNow
            });
        }
    }
}
