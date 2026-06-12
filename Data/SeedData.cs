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
        // These are default starter prompts. Stakeholder-specific prompts can override them through Prompt Inventory.
        var prompts = new (string Name, string Goal, string Inputs, string Outputs, string Location, string Notes, string PromptText)[]
        {
            ("Knowledge Gap Analysis", "Identify what the consultant still does not know after reviewing the client profile and assessment responses.", "Client profile, assessment answers, existing knowledge gaps, notes, documents, transcripts", "JSON knowledge gap items with follow-up questions and sources", "Workspace > Knowledge Gap Analysis", "First AI activity after assessment.", """
                You are an AI readiness consultant assistant. Your task is to identify missing consultant understanding, not to produce conclusions.

                Use the provided client profile and assessment answers to identify what information is still missing before deeper analysis can be performed.

                Focus on unclear or missing information about:
                - business model
                - sales process
                - lead generation
                - customer onboarding
                - CRM ownership
                - system integrations
                - data ownership
                - reporting and KPIs
                - governance and compliance responsibilities
                - operational workflows
                - AI goals, risks, and constraints

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.
                Do not include explanations outside JSON.

                Expected JSON:
                {
                  "items": [
                    {
                      "gapArea": "...",
                      "missingInformation": "...",
                      "whyItMatters": "...",
                      "followUpQuestion": "...",
                      "suggestedEvidence": "...",
                      "priority": "Low|Medium|High",
                      "sources": []
                    }
                  ]
                }

                Rules:
                - Generate 3 to 8 knowledge gaps.
                - If information is missing, say what needs to be clarified.
                - Do not invent facts.
                - Use "Not enough information available" where necessary.
                - sources must always be an array.
                """),
            ("Company Summary", "Create a concise consultant-ready summary of the company based on profile, assessment responses, and approved or answered knowledge gaps.", "Client profile, assessment answers, answered or approved knowledge gaps, consultant notes", "JSON company summary with business model, goals, context, implications, and sources", "Workspace > Company Summary", "Feeds later analysis after approval.", """
                You are an AI readiness consultant assistant. Create a factual, concise company summary for consultant review.

                Use only the provided context:
                - client profile
                - assessment answers
                - answered or approved knowledge gaps
                - consultant notes if available

                Do not invent unsupported facts.
                If information is missing, say "Not enough information available".

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.
                Do not include explanations outside JSON.

                Expected JSON:
                {
                  "summary": "...",
                  "businessModel": "...",
                  "strategicGoals": ["..."],
                  "operationalContext": "...",
                  "aiReadinessImplications": "...",
                  "sources": []
                }

                Rules:
                - summary should be 1 to 2 short paragraphs.
                - businessModel should describe how the company appears to create value.
                - strategicGoals should be a short array.
                - operationalContext should describe key workflows, systems, or operating model if known.
                - aiReadinessImplications should explain what this means for the AI readiness process.
                - sources must always be an array.
                """),
            ("Readiness Score", "Generate interpretation text for the readiness score and the six readiness dimensions.", "Assessment answers, score dimensions, approved evidence", "JSON readiness score interpretation with dimensions and sources", "Workspace > Assessment Responses", "Uses stakeholder score labels.", """
                You are an AI readiness consultant assistant. Interpret the client's AI readiness score using the provided assessment answers and score dimensions.

                Dimensions:
                - Strategy & Vision
                - Data Readiness
                - Technology & Tools
                - People & Culture
                - Governance & Compliance
                - Operations & Process

                Do not invent facts.
                If evidence is weak, say so.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "overallScoreOutOf100": 55,
                  "overallScoreOutOf10": 5.5,
                  "adoptionProfile": "Observer|Cautious Adopter|Leader",
                  "benchmark": "...",
                  "interpretation": "...",
                  "dimensions": [
                    {
                      "area": "Strategy & Vision",
                      "scoreOutOf5": 3,
                      "status": "Critical Gap|Developing|Solid|Strong",
                      "interpretation": "..."
                    }
                  ],
                  "sources": []
                }

                Rules:
                - Keep interpretation consultant-ready.
                - Do not overwrite numeric scores unless provided by context.
                - sources must always be an array.
                """),
            ("Industry Analysis", "Draft a concise industry analysis based on client industry, company summary, and assessment context.", "Company summary, industry, country or market, assessment highlights", "JSON industry analysis with overview, trends, opportunities, risks, implications, and sources", "Workspace > Industry & Competitors", "External sources to be added later.", """
                You are an AI readiness consultant assistant. Draft an industry analysis for consultant review.

                Use the provided company summary, industry, country/market, and assessment highlights.
                Do not perform live web research.
                Do not invent specific statistics unless they are provided in the context.
                If external evidence is missing, state that external validation is required.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "industryOverview": "...",
                  "marketTrends": ["..."],
                  "aiOpportunities": ["..."],
                  "risksAndConstraints": ["..."],
                  "strategicImplications": "...",
                  "sources": []
                }

                Rules:
                - Keep output practical and consultant-ready.
                - sources must always be an array.
                """),
            ("Competitor Analysis", "Draft a competitor analysis using known competitor context and approved prior outputs.", "Approved company summary, approved industry analysis, known competitor names or URLs, assessment highlights", "JSON competitor analysis with competitors, takeaway, and sources", "Workspace > Industry & Competitors", "Requires sourced validation.", """
                You are an AI readiness consultant assistant. Draft a competitor analysis for consultant review.

                Use only the provided context:
                - approved company summary
                - approved industry analysis
                - known competitor names or URLs if available
                - assessment highlights

                Do not invent competitor facts.
                If competitor information is missing, say external research is required.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "competitors": [
                    {
                      "name": "...",
                      "positioning": "...",
                      "aiActivities": "...",
                      "relevanceToClient": "..."
                    }
                  ],
                  "competitiveTakeaway": "...",
                  "sources": []
                }

                Rules:
                - If no competitors are provided, return an empty competitors array and explain in competitiveTakeaway.
                - sources must always be an array.
                """),
            ("SWOT Analysis", "Generate a SWOT analysis from approved prior outputs.", "Approved company summary, industry analysis, competitor analysis, readiness score, assessment highlights", "JSON SWOT analysis with strategic takeaway and sources", "Workspace > SWOT", "Feeds use case identification.", """
                You are an AI readiness consultant assistant. Create a SWOT analysis for consultant review.

                Use:
                - approved company summary
                - approved industry analysis
                - approved competitor analysis
                - readiness score
                - assessment highlights

                Do not invent facts.
                Keep the SWOT specific to AI readiness and business transformation.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "strengths": ["..."],
                  "weaknesses": ["..."],
                  "opportunities": ["..."],
                  "threats": ["..."],
                  "strategicTakeaway": "...",
                  "sources": []
                }

                Rules:
                - 3 to 6 items per SWOT category where possible.
                - sources must always be an array.
                """),
            ("Use Case Identification", "Identify practical AI use cases based on SWOT, readiness gaps, business goals, and operational pain points.", "Approved SWOT, knowledge gaps, readiness dimensions, client goals, pain points, constraints", "JSON use case shortlist with dependencies, expected outcomes, and sources", "Workspace > Use Cases & Scoring", "Future: curated library plus research.", """
                You are an AI readiness consultant assistant. Identify practical AI use cases for consultant review.

                Use:
                - approved SWOT
                - knowledge gaps
                - readiness dimensions
                - client goals
                - operational pain points
                - constraints

                Do not invent unsupported facts.
                Focus on realistic, implementable use cases.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "useCases": [
                    {
                      "name": "...",
                      "description": "...",
                      "focus": "...",
                      "roiPotential": "Low|Medium|High|Very High",
                      "complexity": "Low|Medium|High",
                      "dependencies": ["..."],
                      "expectedOutcome": "...",
                      "sources": []
                    }
                  ]
                }

                Rules:
                - Generate 3 to 6 use cases.
                - Include dependencies clearly.
                - sources must always be an array.
                """),
            ("Use Case Scoring", "Score proposed AI use cases for prioritization.", "Use case shortlist, readiness score, data readiness, risk and compliance constraints, strategic goals", "JSON use case scores with rationale and sources", "Workspace > Use Cases & Scoring", "Consultant can override scores.", """
                You are an AI readiness consultant assistant. Score AI use cases for consultant review.

                Score each use case based on:
                - business value
                - feasibility
                - data readiness
                - implementation complexity
                - risk/compliance sensitivity
                - strategic alignment

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "scores": [
                    {
                      "name": "...",
                      "roiScore": 1,
                      "feasibilityScore": 1,
                      "strategicFitScore": 1,
                      "dataReadinessScore": 1,
                      "riskSafetyScore": 1,
                      "rationale": "..."
                    }
                  ]
                }

                Rules:
                - Scores should be from 1 to 5.
                - Higher roiScore means stronger expected return.
                - Higher riskSafetyScore means a safer risk and compliance posture.
                """),
            ("Roadmap Generation", "Create a phased AI implementation roadmap based on approved use cases and readiness constraints.", "Approved use cases, use case scores, readiness gaps, dependencies, constraints", "JSON roadmap phases with initiatives, success criteria, dependencies, and sources", "Workspace > Roadmap", "Feeds strategic report.", """
                You are an AI readiness consultant assistant. Create a phased AI roadmap for consultant review.

                Use:
                - approved use cases
                - use case scores
                - readiness gaps
                - dependencies
                - constraints

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "phases": [
                    {
                      "phaseName": "Foundation|Pilot & Prove|Scale & Position",
                      "timeframe": "...",
                      "initiatives": ["..."],
                      "successCriteria": ["..."],
                      "dependencies": ["..."]
                    }
                  ],
                  "sources": []
                }

                Rules:
                - Use 3 phases where possible.
                - Keep initiatives practical.
                - sources must always be an array.
                """),
            ("Strategic Report Generation", "Draft strategic report sections using approved workflow outputs.", "Approved workflow outputs and report template sections", "JSON report sections with editable content and sources", "Workspace > Strategic Report", "Final report is not automated without approval.", """
                You are an AI readiness consultant assistant. Draft strategic report sections for consultant review.

                Use only approved outputs:
                - company summary
                - readiness score interpretation
                - knowledge gap outcomes
                - industry analysis
                - competitor analysis
                - SWOT
                - use cases
                - roadmap

                The report should follow this structure:
                1. Personal Note
                2. AI Readiness Summary
                3. Strengths & Development Areas
                4. AI Readiness Deep-Dive
                5. Competitive Snapshot
                6. Top Recommended AI Use Cases
                7. Recommended Roadmap
                8. Next Steps / How We Can Help

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "sections": [
                    {
                      "title": "...",
                      "content": "...",
                      "sources": []
                    }
                  ]
                }

                Rules:
                - Draft content should be editable and consultant-ready.
                - Do not invent facts.
                - Use "External validation required" where external claims need sources.
                - sources must always be an array.
                """),
            ("AI Workspace Refinement", "Improve an existing draft based on consultant feedback while preserving facts and source discipline.", "Current draft, consultant feedback, previous messages, sources", "JSON improved draft, summary of changes, and sources", "AI Workspace", "Used by the consultant chat refinement workflow.", """
                You are assisting an AI readiness consultant.

                Improve the provided draft according to the consultant's feedback.
                Do not invent unsupported facts.
                Do not remove important source references.
                Do not restart from scratch unless explicitly asked.
                Preserve the purpose and structure of the original draft.
                Make the output clearer, more useful, and more consultant-ready.

                Return only valid JSON.
                Do not use markdown.
                Do not use code fences.

                Expected JSON:
                {
                  "improvedDraft": "...",
                  "summaryOfChanges": "...",
                  "sources": []
                }

                Rules:
                - improvedDraft should contain the revised version.
                - summaryOfChanges should briefly explain what changed.
                - sources must always be an array.
                """)
        };

        var existingPrompts = await context.PromptDefinitions
            .ToListAsync();

        foreach (var prompt in prompts)
        {
            var existing = existingPrompts.FirstOrDefault(item =>
                item.PromptName.Equals(prompt.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                context.PromptDefinitions.Add(new PromptDefinition
                {
                    PromptName = prompt.Name,
                    Goal = prompt.Goal,
                    Inputs = prompt.Inputs,
                    Outputs = prompt.Outputs,
                    PlatformLocation = prompt.Location,
                    PromptText = prompt.PromptText,
                    Notes = prompt.Notes,
                    Status = PromptStatus.Active,
                    VersionNumber = 1,
                    CreatedAt = SeedNow
                });
                continue;
            }

            var promptTextNeedsDefault = IsEmptyOrPlaceholder(existing.PromptText);
            if (promptTextNeedsDefault)
            {
                existing.PromptText = prompt.PromptText;
                existing.Status = PromptStatus.Active;
                existing.LastModifiedAt = DateTime.UtcNow;
                existing.VersionNumber = Math.Max(existing.VersionNumber, 1);
            }

            if (IsEmptyOrPlaceholder(existing.Goal))
            {
                existing.Goal = prompt.Goal;
            }

            if (IsEmptyOrPlaceholder(existing.Inputs))
            {
                existing.Inputs = prompt.Inputs;
            }

            if (IsEmptyOrPlaceholder(existing.Outputs))
            {
                existing.Outputs = prompt.Outputs;
            }

            if (IsEmptyOrPlaceholder(existing.PlatformLocation))
            {
                existing.PlatformLocation = prompt.Location;
            }

            if (IsEmptyOrPlaceholder(existing.Notes))
            {
                existing.Notes = prompt.Notes;
            }
        }
    }

    private static bool IsEmptyOrPlaceholder(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            value.Trim().Equals("Stakeholder to provide actual prompt.", StringComparison.OrdinalIgnoreCase);
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
