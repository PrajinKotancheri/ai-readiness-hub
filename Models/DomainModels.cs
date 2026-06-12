using System.ComponentModel.DataAnnotations;

namespace AI_Readiness_Hub.Models;

public class DataProtectionKey
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string FriendlyName { get; set; } = string.Empty;

    [Required]
    public string Xml { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public enum ClientStage
{
    New,
    AssessmentSent,
    AssessmentCompleted,
    KnowledgeGapAnalysis,
    FollowUpDiscovery,
    EvidenceCollected,
    CompanySummary,
    ReadinessScore,
    IndustryAnalysis,
    CompetitorAnalysis,
    SwotAnalysis,
    UseCaseIdentification,
    UseCaseScoring,
    RoadmapGeneration,
    StrategicReport,
    FinalReviewApproved,
    DocumentsUploaded,
    GapAnalysis,
    ConsultantSession,
    ReportDraft,
    InReview,
    Delivered,
    Closed
}

public enum ReportStatus
{
    NotStarted,
    DraftGenerated,
    InConsultantReview,
    NeedsClientClarification,
    ReadyForDelivery,
    Delivered,
    FeedbackReceived,
    Closed
}

public enum WorkflowStepStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

public enum ReadinessFormStatus
{
    NotSent,
    Sent,
    Completed,
    Imported
}

public enum AssessmentResponseSource
{
    GoogleForm,
    ManualImport,
    ClientPortal,
    ExistingImport,
    Other
}

public enum AssessmentResponseStatus
{
    Received,
    Imported,
    Reviewed,
    Ignored
}

public enum CompletenessStatus
{
    Complete,
    Partial,
    Missing
}

public enum DocumentType
{
    CompanyPitch,
    Policy,
    ProcessDocument,
    StrategyDocument,
    DataSample,
    MeetingTranscript,
    AssessmentIntroduction,
    KnowledgeGapDiscoveryMeeting,
    UseCaseWorkshop,
    StrategicReportTemplate,
    ClientReportExample,
    ExistingReport,
    CompetitorDocument,
    Other
}

public enum NoteType
{
    GeneralNote,
    MeetingNote,
    ClientConcern,
    Decision,
    Assumption,
    FollowUp
}

public enum AnalysisType
{
    CompanySummary,
    KnowledgeGapAnalysis,
    GapAnalysis,
    Swot,
    IndustryAnalysis,
    CompetitorInsights,
    UseCaseGeneration,
    UseCaseScoring,
    RiskAnalysis,
    Roadmap,
    ExecutiveSummary,
    FinalReportSection
}

public enum DraftStatus
{
    NotStarted,
    DraftGenerated,
    InReview,
    ConsultantEdited,
    Approved,
    Superseded,
    Rejected,
    NeedsClarification
}

public enum KnowledgeGapArea
{
    StrategyVision,
    BusinessModel,
    SalesQualification,
    CustomerOnboarding,
    ReportingMeasurement,
    DataOwnership,
    SystemsIntegration,
    GovernanceCompliance,
    OperationsProcess,
    Other
}

public enum KnowledgeGapPriority
{
    Low,
    Medium,
    High
}

public enum KnowledgeGapStatus
{
    Draft,
    Open,
    Answered,
    Approved,
    NotRelevant
}

public enum GapArea
{
    BusinessGoals,
    DataReadiness,
    ProcessReadiness,
    TechnologyReadiness,
    AiMaturity,
    Governance,
    Ownership,
    Budget,
    Roi,
    RiskCompliance
}

public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}

public enum GapStatus
{
    Open,
    Clarified,
    Resolved,
    NotApplicable
}

public enum SwotCategory
{
    Strength,
    Weakness,
    Opportunity,
    Threat
}

public enum ItemReviewStatus
{
    Draft,
    Approved,
    Rejected
}

public enum InsightSourceType
{
    ConsultantInput,
    AiGenerated,
    WebResearch,
    ClientDocument
}

public enum InsightStatus
{
    Draft,
    Approved
}

public enum ComplexityLevel
{
    Low,
    Medium,
    High
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum TimeToValue
{
    ZeroToThreeMonths,
    ThreeToSixMonths,
    SixToTwelveMonths,
    TwelvePlusMonths
}

public enum UseCaseStatus
{
    Suggested,
    Shortlisted,
    Approved,
    Rejected,
    FutureOpportunity
}

public enum ScoreCategory
{
    AiBeginner,
    ExplorationReady,
    PilotReady,
    ImplementationReady,
    Observer,
    CautiousAdopter,
    Leader
}

public enum AIOutputType
{
    KnowledgeGap,
    CompanySummary,
    ReadinessScore,
    IndustryAnalysis,
    CompetitorAnalysis,
    SWOT,
    UseCase,
    UseCaseScore,
    Roadmap,
    Report
}

public enum AIOutputSourceType
{
    Internal,
    External
}

public enum AIOutputSourceCategory
{
    Questionnaire,
    AssessmentResponse,
    Document,
    Transcript,
    ConsultantNote,
    Website,
    IndustryReport,
    Research,
    ApprovedOutput,
    Other
}

public enum AIProviderKind
{
    Mock,
    OpenAI
}

public enum AIWorkspaceStatus
{
    Active,
    DraftSaved,
    Approved,
    Closed
}

public enum AIWorkspaceMessageRole
{
    System,
    Consultant,
    Assistant
}

public enum AIOutputRevisionStatus
{
    Draft,
    InReview,
    Approved,
    Superseded,
    Rejected
}

public enum PromptStatus
{
    Draft,
    Active,
    Deprecated
}

public enum ReportTemplateSectionStatus
{
    Draft,
    Active,
    Deprecated
}

public enum RoadmapPhase
{
    ZeroToThreeMonths,
    ThreeToSixMonths,
    SixToTwelveMonths,
    TwelvePlusMonths
}

public enum ApprovalStatus
{
    Draft,
    Approved
}

public enum SectionStatus
{
    NotGenerated,
    DraftGenerated,
    ConsultantEdited,
    Approved
}

public enum TaskType
{
    SendForm,
    Reminder,
    RequestDocument,
    ReviewDocument,
    ScheduleSession,
    UploadTranscript,
    ReviewAiOutput,
    FinalizeReport,
    SendReport,
    FollowUp,
    Other
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum ClientTaskStatus
{
    Open,
    InProgress,
    Done,
    Blocked
}

public class ClientCompany
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Industry { get; set; }

    [Url, StringLength(240)]
    public string? WebsiteUrl { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [StringLength(80)]
    public string? Region { get; set; }

    [StringLength(80)]
    public string? CompanySizeRange { get; set; }

    [StringLength(80)]
    public string? RevenueRange { get; set; }

    [StringLength(120)]
    public string? BusinessModel { get; set; }

    [StringLength(120)]
    public string? ContactPersonName { get; set; }

    [EmailAddress, StringLength(160)]
    public string? ContactPersonEmail { get; set; }

    [Phone, StringLength(80)]
    public string? ContactPersonPhone { get; set; }

    [StringLength(120)]
    public string? ConsultingPackage { get; set; }

    [StringLength(120)]
    public string? AssignedConsultant { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public ClientStage CurrentStage { get; set; } = ClientStage.New;

    [Range(0, 100)]
    public decimal? OverallReadinessScore { get; set; }

    public string? KeyRisksSummary { get; set; }

    [StringLength(240)]
    public string? NextAction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string? CreatedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    [StringLength(120)]
    public string? LastModifiedBy { get; set; }

    public ICollection<ReadinessAssessment> ReadinessAssessments { get; set; } = [];
    public ICollection<ClientWorkflowStep> WorkflowSteps { get; set; } = [];
    public ICollection<ClientDocument> Documents { get; set; } = [];
    public ICollection<ConsultantNote> Notes { get; set; } = [];
    public ICollection<MeetingTranscript> MeetingTranscripts { get; set; } = [];
    public ICollection<AIAnalysisOutput> AnalysisOutputs { get; set; } = [];
    public ICollection<GapAnalysisItem> GapAnalysisItems { get; set; } = [];
    public ICollection<SwotAnalysisItem> SwotItems { get; set; } = [];
    public ICollection<IndustryInsight> IndustryInsights { get; set; } = [];
    public ICollection<CompetitorInsight> CompetitorInsights { get; set; } = [];
    public ICollection<AIUseCase> UseCases { get; set; } = [];
    public ICollection<ReadinessScore> ReadinessScores { get; set; } = [];
    public ICollection<AIRoadmapItem> RoadmapItems { get; set; } = [];
    public ICollection<ClientReport> Reports { get; set; } = [];
    public ICollection<ClientTask> Tasks { get; set; } = [];
    public ICollection<ClientActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<KnowledgeGapItem> KnowledgeGapItems { get; set; } = [];
    public ICollection<AIOutputSource> AIOutputSources { get; set; } = [];
    public ICollection<AIWorkspaceSession> AIWorkspaceSessions { get; set; } = [];
    public ICollection<AIOutputRevision> AIOutputRevisions { get; set; } = [];
}

public class ClientWorkflowStep
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(120)]
    public string StageName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.NotStarted;
    public DateTime? CompletedAt { get; set; }
}

public class ReadinessFormSettings
{
    public int Id { get; set; }

    [Url, StringLength(1000)]
    public string? DefaultFormUrl { get; set; }

    [StringLength(120)]
    public string? ClientReferenceEntryId { get; set; }

    [StringLength(240)]
    public string EmailSubjectTemplate { get; set; } = "AI Readiness Assessment for {{CompanyName}}";

    public string EmailBodyTemplate { get; set; } = """
        Hello {{ContactPersonName}},

        Please complete the AI Readiness Assessment using the link below:

        {{FormLink}}

        This link is unique to your company. Please do not forward it.

        Best regards,
        {{AssignedConsultant}}
        """;

    [StringLength(240)]
    public string? WebhookSecret { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class ReadinessAssessment
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public ReadinessFormStatus FormStatus { get; set; } = ReadinessFormStatus.NotSent;

    [Url, StringLength(500)]
    public string? FormUrl { get; set; }

    [StringLength(80)]
    public string? ClientToken { get; set; }

    [Url, StringLength(1200)]
    public string? GeneratedFormUrl { get; set; }

    [Url, StringLength(1000)]
    public string? CustomFormUrl { get; set; }

    [EmailAddress, StringLength(160)]
    public string? SentToEmail { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? LastReminderSentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ImportedAt { get; set; }
    public DateTime? ResponseReceivedAt { get; set; }

    [StringLength(180)]
    public string? ExternalResponseId { get; set; }

    public string? RawResponseJson { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public ICollection<AssessmentResponse> Responses { get; set; } = [];
    public ICollection<AssessmentAnswer> Answers { get; set; } = [];
}

public class AssessmentResponse
{
    public int Id { get; set; }
    public int ReadinessAssessmentId { get; set; }
    public ReadinessAssessment? ReadinessAssessment { get; set; }
    public int ResponseNumber { get; set; }

    [Required, StringLength(80)]
    public string ResponseLabel { get; set; } = string.Empty;

    public AssessmentResponseSource Source { get; set; } = AssessmentResponseSource.GoogleForm;

    [StringLength(180)]
    public string? ExternalResponseId { get; set; }

    [StringLength(80)]
    public string? ClientToken { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int AnswerCount { get; set; }
    public string? RawResponseJson { get; set; }
    public AssessmentResponseStatus Status { get; set; } = AssessmentResponseStatus.Received;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public ICollection<AssessmentAnswer> Answers { get; set; } = [];
}

public class AssessmentAnswer
{
    public int Id { get; set; }
    public int ReadinessAssessmentId { get; set; }
    public ReadinessAssessment? ReadinessAssessment { get; set; }
    public int? AssessmentResponseId { get; set; }
    public AssessmentResponse? AssessmentResponse { get; set; }

    [Required, StringLength(120)]
    public string SectionName { get; set; } = string.Empty;

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public string? AnswerText { get; set; }

    [StringLength(80)]
    public string? AnswerType { get; set; }

    public bool IsMandatory { get; set; }
    public CompletenessStatus CompletenessStatus { get; set; } = CompletenessStatus.Complete;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ClientDocument
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(240)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? FilePath { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string? UploadedBy { get; set; }

    public string? AiSummary { get; set; }
    public string? KeyInsights { get; set; }
    public bool UsedInReport { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ConsultantNote
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string NoteTitle { get; set; } = string.Empty;

    [Required]
    public string NoteText { get; set; } = string.Empty;

    public NoteType NoteType { get; set; } = NoteType.GeneralNote;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string? CreatedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public int? KnowledgeGapItemId { get; set; }
    public KnowledgeGapItem? KnowledgeGapItem { get; set; }
}

public class MeetingTranscript
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string SessionTitle { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    [Required]
    public string TranscriptText { get; set; } = string.Empty;

    public string? Summary { get; set; }
    public string? KeyDecisions { get; set; }
    public string? FollowUpQuestions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string? CreatedBy { get; set; }

    public int? KnowledgeGapItemId { get; set; }
    public KnowledgeGapItem? KnowledgeGapItem { get; set; }
}

public class AIAnalysisOutput
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public AnalysisType AnalysisType { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    public string? InputSummary { get; set; }
    public string OutputContent { get; set; } = string.Empty;
    public DraftStatus Status { get; set; } = DraftStatus.DraftGenerated;
    public int VersionNumber { get; set; } = 1;
    public DateTime? GeneratedAt { get; set; }

    [StringLength(120)]
    public string? GeneratedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(120)]
    public string? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class GapAnalysisItem
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public GapArea GapArea { get; set; }

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    public string? Impact { get; set; }
    public Severity Severity { get; set; } = Severity.Medium;
    public string? SuggestedFollowUpQuestion { get; set; }
    public string? SuggestedAction { get; set; }
    public GapStatus Status { get; set; } = GapStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class SwotAnalysisItem
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public SwotCategory Category { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? EvidenceSource { get; set; }
    public string? ConsultantComment { get; set; }
    public ItemReviewStatus Status { get; set; } = ItemReviewStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class IndustryInsight
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string InsightText { get; set; } = string.Empty;

    public string? Relevance { get; set; }
    public InsightSourceType SourceType { get; set; } = InsightSourceType.ConsultantInput;
    public InsightStatus Status { get; set; } = InsightStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CompetitorInsight
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string CompetitorName { get; set; } = string.Empty;

    [Url, StringLength(240)]
    public string? WebsiteUrl { get; set; }

    [Required]
    public string InsightText { get; set; } = string.Empty;

    public string? AiUseCasesObserved { get; set; }
    public string? StrengthComparedToClient { get; set; }
    public string? WeaknessComparedToClient { get; set; }
    public string? SourceNotes { get; set; }
    public InsightStatus Status { get; set; } = InsightStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AIUseCase
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? BusinessProblem { get; set; }

    [StringLength(120)]
    public string? Department { get; set; }

    public string? ExpectedBenefit { get; set; }
    public string? RequiredData { get; set; }
    public ComplexityLevel ImplementationComplexity { get; set; } = ComplexityLevel.Medium;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    public TimeToValue TimeToValue { get; set; } = TimeToValue.ThreeToSixMonths;
    public UseCaseStatus Status { get; set; } = UseCaseStatus.Suggested;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public AIUseCaseScore? Score { get; set; }
}

public class AIUseCaseScore
{
    public int Id { get; set; }
    public int AIUseCaseId { get; set; }
    public AIUseCase? AIUseCase { get; set; }

    [Range(1, 5)]
    public int RoiScore { get; set; } = 3;

    [Range(1, 5)]
    public int FeasibilityScore { get; set; } = 3;

    [Range(1, 5)]
    public int RiskSafetyScore { get; set; } = 3;

    [Range(1, 5)]
    public int StrategicFitScore { get; set; } = 3;

    [Range(1, 5)]
    public int DataReadinessScore { get; set; } = 3;

    public decimal PriorityScore { get; set; }
    public string? ScoringComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }

    public void RecalculatePriority()
    {
        PriorityScore = Math.Round(
            0.30m * RoiScore +
            0.25m * FeasibilityScore +
            0.20m * StrategicFitScore +
            0.15m * DataReadinessScore +
            0.10m * RiskSafetyScore,
            2);
    }
}

public class ReadinessScore
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Range(0, 100)]
    public int BusinessClarityScore { get; set; }

    [Range(0, 100)]
    public int DataReadinessScore { get; set; }

    [Range(0, 100)]
    public int ProcessReadinessScore { get; set; }

    [Range(0, 100)]
    public int TechnologyReadinessScore { get; set; }

    [Range(0, 100)]
    public int PeopleGovernanceScore { get; set; }

    [Range(0, 100)]
    public int? GovernanceComplianceScore { get; set; }

    [Range(0, 100)]
    public int OverallScore { get; set; }

    public ScoreCategory ScoreCategory { get; set; } = ScoreCategory.AiBeginner;
    public string? ScoringSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class AIRoadmapItem
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public RoadmapPhase Phase { get; set; } = RoadmapPhase.ZeroToThreeMonths;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? RelatedUseCaseId { get; set; }
    public AIUseCase? RelatedUseCase { get; set; }

    [StringLength(120)]
    public string? Owner { get; set; }

    public string? ExpectedOutcome { get; set; }
    public string? Dependencies { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class ClientReport
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(220)]
    public string ReportTitle { get; set; } = string.Empty;

    public ReportStatus ReportStatus { get; set; } = ReportStatus.NotStarted;
    public int VersionNumber { get; set; } = 1;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? FinalReportContent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public ICollection<ReportSection> Sections { get; set; } = [];
}

public class ReportSection
{
    public int Id { get; set; }
    public int ClientReportId { get; set; }
    public ClientReport? ClientReport { get; set; }

    [Required, StringLength(180)]
    public string SectionTitle { get; set; } = string.Empty;

    public int SectionOrder { get; set; }
    public string? SectionContent { get; set; }
    public SectionStatus SectionStatus { get; set; } = SectionStatus.NotGenerated;
    public DateTime? ApprovedAt { get; set; }

    [StringLength(120)]
    public string? ApprovedBy { get; set; }

    public string? SourceSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class KnowledgeGapItem
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public int? AssessmentResponseId { get; set; }
    public AssessmentResponse? AssessmentResponse { get; set; }

    public KnowledgeGapArea GapArea { get; set; } = KnowledgeGapArea.Other;

    [Required]
    public string MissingInformation { get; set; } = string.Empty;

    public string? WhyItMatters { get; set; }
    public string? FollowUpQuestion { get; set; }
    public string? SuggestedEvidence { get; set; }
    public KnowledgeGapPriority Priority { get; set; } = KnowledgeGapPriority.Medium;
    public KnowledgeGapStatus Status { get; set; } = KnowledgeGapStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [StringLength(120)]
    public string? ApprovedBy { get; set; }
}

public class AIOutputSource
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public AIOutputType OutputType { get; set; }
    public int? OutputId { get; set; }
    public AIOutputSourceType SourceType { get; set; } = AIOutputSourceType.Internal;
    public AIOutputSourceCategory SourceCategory { get; set; } = AIOutputSourceCategory.Other;

    [Required, StringLength(220)]
    public string SourceLabel { get; set; } = string.Empty;

    [StringLength(260)]
    public string? SourceReference { get; set; }

    [Url, StringLength(1000)]
    public string? SourceUrl { get; set; }

    public string? EvidenceText { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PromptDefinition
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string PromptName { get; set; } = string.Empty;

    public string? Goal { get; set; }
    public string? Inputs { get; set; }
    public string? Outputs { get; set; }

    [StringLength(160)]
    public string? PlatformLocation { get; set; }

    public string PromptText { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public PromptStatus Status { get; set; } = PromptStatus.Draft;
    public int VersionNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class ReportTemplateSection
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string SectionTitle { get; set; } = string.Empty;

    public int SectionOrder { get; set; }
    public string? SectionGoal { get; set; }
    public string? DefaultPrompt { get; set; }
    public ReportTemplateSectionStatus Status { get; set; } = ReportTemplateSectionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class UseCaseLibraryItem
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ApplicableIndustries { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? TypicalRoi { get; set; }
    public string? Evidence { get; set; }
    public string? CaseStudies { get; set; }
    public ComplexityLevel Complexity { get; set; } = ComplexityLevel.Medium;
    public string? Dependencies { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class AIWorkspaceSession
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public AIOutputType OutputType { get; set; }
    public int? OutputId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    public AIWorkspaceStatus Status { get; set; } = AIWorkspaceStatus.Active;
    public AIProviderKind Provider { get; set; } = AIProviderKind.Mock;

    [StringLength(120)]
    public string? Model { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [StringLength(120)]
    public string? ApprovedBy { get; set; }

    public ICollection<AIWorkspaceMessage> Messages { get; set; } = [];
    public ICollection<AIOutputRevision> Revisions { get; set; } = [];
}

public class AIWorkspaceMessage
{
    public int Id { get; set; }
    public int AIWorkspaceSessionId { get; set; }
    public AIWorkspaceSession? AIWorkspaceSession { get; set; }
    public AIWorkspaceMessageRole Role { get; set; } = AIWorkspaceMessageRole.Consultant;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    public string? DraftContentSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AIOutputRevision
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }
    public AIOutputType OutputType { get; set; }
    public int? OutputId { get; set; }
    public int? AIWorkspaceSessionId { get; set; }
    public AIWorkspaceSession? AIWorkspaceSession { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string DraftContent { get; set; } = string.Empty;
    public string? ConsultantFeedback { get; set; }
    public AIProviderKind Provider { get; set; } = AIProviderKind.Mock;

    [StringLength(120)]
    public string? Model { get; set; }

    public AIOutputRevisionStatus Status { get; set; } = AIOutputRevisionStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    [StringLength(120)]
    public string? ApprovedBy { get; set; }
}

public class ClientTask
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(180)]
    public string TaskTitle { get; set; } = string.Empty;

    public string? TaskDescription { get; set; }
    public TaskType TaskType { get; set; } = TaskType.Other;

    [StringLength(120)]
    public string? AssignedTo { get; set; }

    public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public ClientTaskStatus Status { get; set; } = ClientTaskStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class ClientActivityLog
{
    public int Id { get; set; }
    public int ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    [Required, StringLength(120)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(120)]
    public string? CreatedBy { get; set; }
}
